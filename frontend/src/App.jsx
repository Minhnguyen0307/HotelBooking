import { useEffect } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './auth';
import { AccountMarks } from './lux';

export default function App() {
  const { user, logout, hasRole } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const flash = location.state?.message;
  const home = location.pathname === '/'
    || location.pathname === '/search'
    || location.pathname.startsWith('/rooms/')
    || location.pathname.startsWith('/pay/')
    || location.pathname === '/login';
  const staff = hasRole('Receptionist', 'Manager', 'Admin');
  const manager = hasRole('Manager', 'Admin');

  function onLogout() {
    logout();
    navigate('/login');
  }

  useEffect(() => {
    if (location.pathname !== '/' || !user) return;
    if (manager) navigate('/reports', { replace: true });
    else if (staff) navigate('/frontdesk', { replace: true });
  }, [location.pathname, user, manager, staff, navigate]);

  return (
    <div className={`shell${home ? ' is-home' : ''}`}>
      {!home && <div className="topbar">
        <span>Chào mừng quý khách đến HBS</span>
        <span>Check-in 14:00 · Check-out 12:00</span>
      </div>}
      {!home && staff && <nav className="nav">
        <NavLink className="brand" to={manager ? '/reports' : '/frontdesk'}><span className="brand-mark">H</span>HBS <span>Desk</span></NavLink>
        <div className="menu">
          <NavLink to="/frontdesk">Lễ tân</NavLink>
          {manager && (
            <>
              <NavLink to="/reports">Báo cáo</NavLink>
              <NavLink to="/admin/rooms">Phòng</NavLink>
              <NavLink to="/admin/services">Dịch vụ</NavLink>
              <NavLink to="/admin/promos">Mã giảm</NavLink>
              <NavLink to="/admin/customers">Khách</NavLink>
              <NavLink to="/admin/staff">Nhân viên</NavLink>
            </>
          )}
          <span className="work-name">{user?.fullName}</span>
          <button className="text-btn" type="button" onClick={onLogout}>Đăng xuất</button>
        </div>
      </nav>}
      {!home && !staff && <nav className="nav">
        <NavLink className="brand" to="/"><span className="brand-mark">H</span>HBS <span>Hotel</span></NavLink>
        <div className="menu">
          <NavLink to="/" end>Trang chủ</NavLink>
          <NavLink to="/search">Tìm phòng</NavLink>
          {user ? <AccountMarks user={user} /> : (
            <>
              <NavLink className="nav-cta" to="/login">Đăng nhập</NavLink>
              <NavLink to="/register">Đăng ký</NavLink>
            </>
          )}
        </div>
      </nav>}
      <main>
        {flash && <div className={`banner ${location.state.kind || 'ok'}`}>{flash}</div>}
        <Outlet />
      </main>
      {!home && <footer className="footer">
        <span>{staff ? 'HBS Desk · Đà Nẵng' : 'HBS Luxury Hotel · Đà Nẵng'}</span>
        {!staff && <NavLink to="/privacy">Bảo mật</NavLink>}
      </footer>}
    </div>
  );
}
