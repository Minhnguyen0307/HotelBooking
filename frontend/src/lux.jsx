import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from './auth';

function HistoryMark() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <rect x="4" y="5" width="16" height="15" rx="2" fill="none" stroke="currentColor" strokeWidth="1.6" />
      <path d="M4 9.5h16M8 3.5v3M16 3.5v3M8 14h3" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
    </svg>
  );
}

export function AccountMarks({ user, onNavigate }) {
  const initial = (user.fullName || 'K').trim().charAt(0).toUpperCase();
  return (
    <>
      <Link className="lux-ico" to="/bookings" aria-label="Lịch sử đặt phòng" title="Lịch sử đặt phòng" onClick={onNavigate}>
        <HistoryMark />
      </Link>
      <Link className="lux-ico lux-who" to="/profile" title="Hồ sơ" onClick={onNavigate}>
        <span className="lux-avatar" aria-hidden="true">{initial}</span>
        <span className="lux-who-name">{user.fullName}</span>
      </Link>
    </>
  );
}

export function LuxBar({ solid = false, home = false }) {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const [scrolled, setScrolled] = useState(solid);

  useEffect(() => {
    if (solid) return undefined;
    function onScroll() { setScrolled(window.scrollY > 60); }
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, [solid]);

  const href = (hash) => (home ? hash : `/${hash}`);

  function bookNow(event) {
    if (user) return;
    event.preventDefault();
    navigate('/login', { state: { from: '/search' } });
  }

  return (
    <header className={`lux-nav${solid || scrolled ? ' is-solid' : ''}`}>
      <div className="lux-nav-inner">
        <Link className="lux-logo" to="/">
          <span className="lux-mark">H</span>
          <span>
            <strong>HBS</strong>
            <small>Hotels & Resorts</small>
          </span>
        </Link>
        <nav className="lux-links">
          <Link to="/">Trang chủ</Link>
          <a href={href('#branches')}>Chi nhánh</a>
          <a href={href('#rooms')}>Hạng phòng</a>
          <a href={href('#experiences')}>Trải nghiệm</a>
          <a href={href('#offers')}>Ưu đãi</a>
        </nav>
        <div className="lux-actions">
          {user ? <AccountMarks user={user} /> : (
            <>
              <Link to="/login">Đăng nhập</Link>
              <Link to="/register">Đăng ký</Link>
            </>
          )}
          <Link className="lux-book" to="/search" onClick={bookNow}>Đặt phòng ngay</Link>
          <button className="lux-burger" type="button" aria-label="Mở menu" onClick={() => setMenuOpen((open) => !open)}>☰</button>
        </div>
      </div>
      {menuOpen && (
        <div className="lux-drawer">
          <a href={href('#branches')} onClick={() => setMenuOpen(false)}>Chi nhánh</a>
          <a href={href('#rooms')} onClick={() => setMenuOpen(false)}>Hạng phòng</a>
          <Link to="/search" onClick={(event) => { setMenuOpen(false); bookNow(event); }}>Đặt phòng</Link>
          {user ? (
            <>
              <Link to="/bookings" onClick={() => setMenuOpen(false)}>Lịch sử đặt phòng</Link>
              <Link to="/profile" onClick={() => setMenuOpen(false)}>{user.fullName}</Link>
            </>
          ) : (
            <>
              <Link to="/login" onClick={() => setMenuOpen(false)}>Đăng nhập</Link>
              <Link to="/register" onClick={() => setMenuOpen(false)}>Đăng ký</Link>
            </>
          )}
        </div>
      )}
    </header>
  );
}
