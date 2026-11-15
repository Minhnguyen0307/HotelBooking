import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';
import { AsyncView, useLoad } from '../ui';

export function ProfilePage() {
  const { user, setUser, logout } = useAuth();
  const navigate = useNavigate();
  const state = useLoad(() => api('/api/auth/profile'), []);
  const [error, setError] = useState('');

  function onLogout() {
    logout();
    navigate('/');
  }

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/auth/profile', {
        method: 'PUT',
        body: { ...data, email: user.email }
      });
      setUser({ ...user, fullName: data.fullName });
      navigate('/profile', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(profile) => (
        <section className="panel account-card profile-card">
          <div className="profile-head">
            <span className="profile-mark">{(profile.fullName || 'K').trim().charAt(0).toUpperCase()}</span>
            <div>
              <p className="eyebrow">Khách hàng</p>
              <h1>{profile.fullName}</h1>
            </div>
          </div>
          <dl className="profile-facts">
            <div><dt>Họ tên</dt><dd>{profile.fullName}</dd></div>
            <div><dt>Email</dt><dd>{profile.email}</dd></div>
            <div><dt>Số điện thoại</dt><dd>{profile.phoneNumber || 'Chưa cập nhật'}</dd></div>
          </dl>
          <form className="stack" onSubmit={onSubmit}>
            <h2>Cập nhật thông tin</h2>
            {error && <div className="banner">{error}</div>}
            <label>Họ tên<input name="fullName" defaultValue={profile.fullName} required /></label>
            <label>Số điện thoại<input name="phoneNumber" defaultValue={profile.phoneNumber || ''} /></label>
            <button className="btn" type="submit">Lưu</button>
          </form>
          <div className="profile-links">
            {user?.role === 'Customer' && <Link to="/offers">Ưu đãi của tôi</Link>}
            <Link to="/password">Đổi mật khẩu</Link>
            <button className="text-btn" type="button" onClick={onLogout}>Đăng xuất</button>
          </div>
        </section>
      )}
    </AsyncView>
  );
}
