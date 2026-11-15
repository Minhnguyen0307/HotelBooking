import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';
import { AsyncView, useLoad } from '../ui';

export function ProfilePage() {
  const { user, setUser } = useAuth();
  const navigate = useNavigate();
  const state = useLoad(() => api('/api/auth/profile'), []);
  const [error, setError] = useState('');

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
        <form className="panel stack account-card" onSubmit={onSubmit}>
          <h1>Hồ sơ</h1>
          {error && <div className="banner">{error}</div>}
          <p className="muted">{profile.email} · {user.role}</p>
          <label>Họ tên<input name="fullName" defaultValue={profile.fullName} required /></label>
          <label>Số điện thoại<input name="phoneNumber" defaultValue={profile.phoneNumber || ''} /></label>
          <button className="btn" type="submit">Lưu</button>
          <Link to="/password">Đổi mật khẩu</Link>
        </form>
      )}
    </AsyncView>
  );
}
