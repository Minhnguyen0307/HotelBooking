import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './auth';
import { AccountMarks } from './lux';

export default function App() {
  const { user, hasRole } = useAuth();
  const location = useLocation();
  const flash = location.state?.message;
  const home = location.pathname === '/'
    || location.pathname === '/search'
    || location.pathname.startsWith('/rooms/')
    || location.pathname.startsWith('/pay/')
    || location.pathname === '/login';

  return (
    <div className={`shell${home ? ' is-home' : ''}`}>
      {!home && <div className="topbar">
        <span>Chào mừng quý khách đến HBS</span>
        <span>Check-in 14:00 · Check-out 12:00</span>
      </div>}
      {!home && <nav className="nav">
        <NavLink className="brand" to="/"><span className="brand-mark">H</span>HBS <span>Hotel</span></NavLink>
        <div className="menu">
          <NavLink to="/" end>Trang chủ</NavLink>
          <NavLink to="/search">Tìm phòng</NavLink>
          {hasRole('Receptionist', 'Manager', 'Admin') && <NavLink to="/frontdesk">Lễ tân</NavLink>}
          {hasRole('Manager', 'Admin') && (
            <>
              <NavLink to="/admin/rooms">Phòng</NavLink>
              <NavLink to="/admin/types">Loại phòng</NavLink>
              <NavLink to="/admin/customers">Khách</NavLink>
              <NavLink to="/reports">Báo cáo</NavLink>
            </>
          )}
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
        <span>HBS Luxury Hotel · Đà Nẵng</span>
        <NavLink to="/privacy">Bảo mật</NavLink>
      </footer>}
    </div>
  );
}
