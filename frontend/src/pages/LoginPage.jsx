import { useEffect, useRef, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';

const HERO = 'https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=1200&q=80';

function loadGoogleScript() {
  if (window.google?.accounts?.id) return Promise.resolve();
  return new Promise((resolve, reject) => {
    const existing = document.querySelector('script[data-google-gsi]');
    if (existing) {
      existing.addEventListener('load', () => resolve(), { once: true });
      existing.addEventListener('error', () => reject(new Error('Không tải được đăng nhập Google.')), { once: true });
      return;
    }
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.dataset.googleGsi = '1';
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('Không tải được đăng nhập Google.'));
    document.head.appendChild(script);
  });
}

export function LoginPage() {
  const { login, loginWithGoogle } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState('');
  const [staff, setStaff] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const buttonRef = useRef(null);
  const fromRef = useRef('/');
  fromRef.current = location.state?.from || '/';

  useEffect(() => {
    let cancelled = false;

    async function setup() {
      const config = await api('/api/auth/google-client');
      if (cancelled || !config.clientId || !buttonRef.current) return;
      await loadGoogleScript();
      if (cancelled || !buttonRef.current || !window.google?.accounts?.id) return;

      window.google.accounts.id.initialize({
        client_id: config.clientId,
        callback: async (response) => {
          setError('');
          try {
            const account = await loginWithGoogle(response.credential);
            navigate(fromRef.current !== '/' ? fromRef.current : account?.role === 'Receptionist' ? '/frontdesk' : '/', { replace: true });
          } catch (err) {
            setError(err.message);
          }
        }
      });
      buttonRef.current.innerHTML = '';
      window.google.accounts.id.renderButton(buttonRef.current, {
        theme: 'outline',
        size: 'large',
        text: 'signin_with',
        shape: 'pill',
        width: 320
      });
    }

    setup().catch(() => {});

    return () => {
      cancelled = true;
    };
  }, [loginWithGoogle, navigate]);

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    setBusy(true);
    try {
      const account = await login(data.email, data.password);
      const requested = location.state?.from;
      navigate(requested || (account?.role === 'Receptionist' ? '/frontdesk' : '/'), { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-screen">
      <header className="auth-top">
        <Link className="lux-logo" to="/">
          <span className="lux-mark">H</span>
          <span>
            <strong>HBS HOTEL</strong>
            <small>Luxury Resorts · Đà Nẵng</small>
          </span>
        </Link>
        <Link to="/">← Trang chủ</Link>
      </header>
      <main>
        <section className="auth-card">
          <aside>
            <img src={HERO} alt="HBS Luxury Hotel Đà Nẵng" />
            <div>
              <p>HBS Privé</p>
              <h2>Khởi đầu hành trình <span>nghỉ dưỡng đẳng cấp</span></h2>
              <p className="auth-copy">Đăng nhập để giữ phòng, xem đơn và tiếp tục kỳ nghỉ tại năm cơ sở HBS ở Đà Nẵng.</p>
              <ul>
                <li>Ưu đãi WELCOME10 khi đặt trực tiếp</li>
                <li>Quản lý đơn và thanh toán trên tài khoản của bạn</li>
              </ul>
            </div>
          </aside>
          <div className="auth-form">
            <div className="auth-roles">
              <button type="button" className={staff ? '' : 'on'} onClick={() => setStaff(false)}>Khách hàng</button>
              <button type="button" className={staff ? 'on' : ''} onClick={() => setStaff(true)}>Nhân viên</button>
            </div>
            <p className="lux-kicker dark">{staff ? 'Cổng lễ tân HBS' : 'Cổng thành viên HBS'}</p>
            <h1>Chào mừng trở lại</h1>
            <p>{staff ? 'Đăng nhập tài khoản nhân viên để vào quầy lễ tân hoặc báo cáo.' : 'Mời bạn đăng nhập để tiếp tục kỳ nghỉ và quản lý đặt phòng.'}</p>
            {error && <div className="banner">{error}</div>}
            <form onSubmit={onSubmit}>
              <label>Email đăng nhập
                <input name="email" type="email" required placeholder={staff ? 'staff@hotel.com' : 'khachhang@hotel.com'} />
              </label>
              <label>Mật khẩu
                <span className="auth-pass">
                  <input name="password" type={showPassword ? 'text' : 'password'} required placeholder="••••••••" />
                  <button type="button" onClick={() => setShowPassword((value) => !value)}>{showPassword ? 'Ẩn' : 'Hiện'}</button>
                </span>
              </label>
              <div className="auth-row">
                <Link to="/forgot">Quên mật khẩu?</Link>
              </div>
              <button className="lux-navy" type="submit" disabled={busy}>{busy ? 'Đang xác thực...' : 'Đăng nhập'}</button>
            </form>
            <div className={staff ? 'auth-social is-hidden' : 'auth-social'}>
              <p>hoặc</p>
              <div className="google-login" ref={buttonRef} />
              <p>Chưa là hội viên? <Link to="/register" state={{ from: location.state?.from }}>Tạo tài khoản mới</Link></p>
            </div>
            {staff && <p className="auth-hint">Dùng tài khoản lễ tân, quản lý hoặc quản trị được cấp để vào quầy và buồng phòng.</p>}
          </div>
        </section>
      </main>
    </div>
  );
}
