import { useEffect, useRef, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';

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
  const [googleReady, setGoogleReady] = useState(false);
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
            await loginWithGoogle(response.credential);
            navigate(fromRef.current, { replace: true });
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
      if (!cancelled) setGoogleReady(true);
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
    try {
      await login(data.email, data.password);
      navigate(location.state?.from || '/', { replace: true });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <form className="panel stack account-card" onSubmit={onSubmit}>
      <h1>Đăng nhập</h1>
      {error && <div className="banner">{error}</div>}
      <label>Email<input name="email" type="email" required /></label>
      <label>Mật khẩu<input name="password" type="password" required /></label>
      <button className="btn" type="submit">Vào hệ thống</button>
      <div className="or-line">hoặc</div>
      <div className="google-login" ref={buttonRef} />
      {!googleReady && <p className="muted">Nút Google hiện sau khi điền Client ID vào appsettings và chạy lại API.</p>}
      <p className="muted"><Link to="/forgot">Quên mật khẩu</Link> · <Link to="/register">Tạo tài khoản</Link></p>
      <div className="banner info">Tài khoản mẫu: customer1@hotel.com / Customer@123 · admin@hotel.com / Admin@123 · receptionist@hotel.com / Reception@123</div>
    </form>
  );
}
