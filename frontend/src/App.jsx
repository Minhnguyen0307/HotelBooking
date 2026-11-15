import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './auth';

export default function App() {
  const { user, logout, hasRole } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const flash = location.state?.message;

  function onLogout() {
    logout();
    navigate('/');
  }

  return (
    <div className="shell">
      <div className="topbar">
        <span>HBS Luxury Hotel · 123 Võ Nguyên Giáp, Sơn Trà, Đà Nẵng</span>
        <span>Check-in 14:00 · Check-out 12:00</span>
      </div>
      <nav className="nav">
        <NavLink className="brand" to="/">HBS <span>Hotel</span></NavLink>
        <div className="menu">
          <NavLink to="/" end>Trang chủ</NavLink>
          <NavLink to="/search">Tìm phòng</NavLink>
          {user && <NavLink to="/bookings">Đơn của tôi</NavLink>}
          {hasRole('Receptionist', 'Manager', 'Admin') && <NavLink to="/frontdesk">Lễ tân</NavLink>}
          {hasRole('Manager', 'Admin') && (
            <>
              <NavLink to="/admin/rooms">Phòng</NavLink>
              <NavLink to="/admin/types">Loại phòng</NavLink>
              <NavLink to="/admin/customers">Khách</NavLink>
              <NavLink to="/reports">Báo cáo</NavLink>
            </>
          )}
          {user ? (
            <>
              <NavLink to="/profile">{user.fullName}</NavLink>
              <button className="text-btn" type="button" onClick={onLogout}>Đăng xuất</button>
            </>
          ) : (
            <>
              <NavLink to="/login">Đăng nhập</NavLink>
              <NavLink to="/register">Đăng ký</NavLink>
            </>
          )}
        </div>
      </nav>
      <main>
        {flash && <div className={`banner ${location.state.kind || 'ok'}`}>{flash}</div>}
        <Outlet />
      </main>
      <footer className="footer">
        HBS Luxury Hotel · React + Web API · <NavLink to="/privacy">Bảo mật</NavLink>
      </footer>
    </div>
  );
}
