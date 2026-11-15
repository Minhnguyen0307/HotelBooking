import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, useLoad } from '../ui';

const roles = [
  ['Receptionist', 'Lễ tân'],
  ['Manager', 'Quản lý'],
  ['Admin', 'Quản trị']
];

export function StaffPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/admin/staff'), [tick]);

  function done(message) {
    setTick((value) => value + 1);
    navigate('/admin/staff', { state: { message, kind: 'ok' } });
  }

  async function create(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/admin/staff', { method: 'POST', body: data });
      event.target.reset();
      done(result.message);
    } catch (err) {
      setError(err.message);
    }
  }

  async function save(event, id) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api(`/api/admin/staff/${id}`, {
        method: 'PUT',
        body: { ...data, isActive: data.isActive === 'true' }
      });
      done(result.message);
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(staff) => (
        <>
          <header className="page-head">
            <p className="eyebrow">Quản trị</p>
            <h1>Nhân viên</h1>
          </header>
          {error && <div className="banner">{error}</div>}
          <form className="panel form-grid" onSubmit={create}>
            <label>Họ tên<input name="fullName" required /></label>
            <label>Email<input name="email" type="email" required /></label>
            <label>Số điện thoại<input name="phoneNumber" placeholder="0901234567" /></label>
            <label>Vai trò
              <select name="roleName" defaultValue="Receptionist">
                {roles.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label>Mật khẩu<input name="password" type="password" minLength="8" required /></label>
            <button className="btn" type="submit">Thêm</button>
          </form>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Tên</th><th>Email</th><th>Điện thoại</th><th>Vai trò</th><th>Trạng thái</th><th></th></tr></thead>
              <tbody>
                {staff.length === 0 && <tr><td colSpan="6">Chưa có nhân viên.</td></tr>}
                {staff.map((person) => (
                  <tr key={person.userId}>
                    <td><input form={`staff-${person.userId}`} name="fullName" defaultValue={person.fullName} required /></td>
                    <td>{person.email}</td>
                    <td><input form={`staff-${person.userId}`} name="phoneNumber" defaultValue={person.phoneNumber || ''} /></td>
                    <td>
                      <select form={`staff-${person.userId}`} name="roleName" defaultValue={person.roleName}>
                        {roles.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                      </select>
                    </td>
                    <td>
                      <select form={`staff-${person.userId}`} name="isActive" defaultValue={person.isActive ? 'true' : 'false'}>
                        <option value="true">Đang làm</option>
                        <option value="false">Đã khóa</option>
                      </select>
                    </td>
                    <td><button className="btn small" form={`staff-${person.userId}`} type="submit">Lưu</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {staff.map((person) => (
            <form id={`staff-${person.userId}`} key={`form-${person.userId}`} onSubmit={(event) => save(event, person.userId)} />
          ))}
        </>
      )}
    </AsyncView>
  );
}
