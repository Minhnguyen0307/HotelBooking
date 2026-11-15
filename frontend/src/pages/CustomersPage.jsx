import { useState } from 'react';
import { api } from '../api';
import { AsyncView, useLoad } from '../ui';

function roleBadge(roleName) {
  switch (roleName) {
    case 'Admin':
      return <span className="pill" style={{ background: '#9d3b32', color: 'white', fontWeight: 600 }}>Admin</span>;
    case 'Manager':
      return <span className="pill" style={{ background: '#b5812f', color: 'white', fontWeight: 600 }}>Quản lý</span>;
    case 'Receptionist':
      return <span className="pill" style={{ background: '#1a2b4c', color: 'white', fontWeight: 600 }}>Lễ tân</span>;
    default:
      return <span className="pill ok">Khách hàng</span>;
  }
}

export function CustomersPage() {
  const [tick, setTick] = useState(0);
  const [filterRole, setFilterRole] = useState('all');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);

  const state = useLoad(() => api('/api/admin/customers'), [tick]);
  const rolesState = useLoad(() => api('/api/admin/customers/roles'), []);

  function refresh(msg) {
    setError('');
    setNotice(msg || '');
    setTick((t) => t + 1);
  }

  async function toggle(id) {
    setError('');
    try {
      const result = await api(`/api/admin/customers/${id}/toggle`, { method: 'POST' });
      refresh(result.message);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleRoleChange(userId, newRoleId) {
    if (!window.confirm('Bạn có chắc chắn muốn thay đổi vai trò của người dùng này?')) {
      setTick((t) => t + 1);
      return;
    }
    setBusy(true);
    setError('');
    try {
      const res = await api(`/api/admin/customers/${userId}/role`, {
        method: 'PUT',
        body: { roleId: Number(newRoleId) },
      });
      refresh(res.message);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="stack" style={{ maxWidth: 1100, margin: '0 auto' }}>
      <div className="spread">
        <div>
          <h1>Quản lý Người dùng & Phân quyền</h1>
          <p className="muted">Quản trị toàn bộ tài khoản khách hàng, nhân viên lễ tân, quản lý và phân quyền hệ thống.</p>
        </div>
      </div>

      {notice && <div className="banner ok">{notice}</div>}
      {error && <div className="banner">{error}</div>}

      <AsyncView state={state}>
        {(users) => {
          const allUsers = users || [];
          const roles = rolesState.data || [];

          // Counts
          const totalCount = allUsers.length;
          const customerCount = allUsers.filter((u) => u.roleName === 'Customer').length;
          const staffCount = allUsers.filter((u) => u.roleName !== 'Customer').length;

          // Filter
          const filtered = filterRole === 'all'
            ? allUsers
            : filterRole === 'staff'
              ? allUsers.filter((u) => u.roleName !== 'Customer')
              : allUsers.filter((u) => u.roleName === filterRole);

          return (
            <>
              {/* Filter Tabs */}
              <div className="tabs" style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem', flexWrap: 'wrap' }}>
                <button
                  type="button"
                  className={`tab-btn ${filterRole === 'all' ? 'active' : ''}`}
                  onClick={() => setFilterRole('all')}
                >
                  Tất cả ({totalCount})
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filterRole === 'staff' ? 'active' : ''}`}
                  onClick={() => setFilterRole('staff')}
                >
                  Nhân sự & Quản trị ({staffCount})
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filterRole === 'Customer' ? 'active' : ''}`}
                  onClick={() => setFilterRole('Customer')}
                >
                  Khách hàng ({customerCount})
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filterRole === 'Receptionist' ? 'active' : ''}`}
                  onClick={() => setFilterRole('Receptionist')}
                >
                  Lễ tân
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filterRole === 'Manager' ? 'active' : ''}`}
                  onClick={() => setFilterRole('Manager')}
                >
                  Quản lý
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filterRole === 'Admin' ? 'active' : ''}`}
                  onClick={() => setFilterRole('Admin')}
                >
                  Admin
                </button>
              </div>

              <div className="table-wrap panel">
                <table>
                  <thead>
                    <tr>
                      <th>Họ tên</th>
                      <th>Email / SĐT</th>
                      <th>Vai trò hiện tại</th>
                      <th>Phân quyền vai trò</th>
                      <th>Trạng thái</th>
                      <th style={{ textAlign: 'right' }}>Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filtered.length === 0 && (
                      <tr>
                        <td colSpan="6" style={{ textAlign: 'center', padding: '2rem' }}>
                          Không có người dùng nào phù hợp với bộ lọc.
                        </td>
                      </tr>
                    )}
                    {filtered.map((user) => (
                      <tr key={user.userId}>
                        <td>
                          <strong>{user.fullName}</strong>
                          <div style={{ fontSize: '0.8rem', color: 'var(--muted)' }}>
                            ID #{user.userId} · Tham gia {new Date(user.createdAt).toLocaleDateString('vi-VN')}
                          </div>
                        </td>
                        <td>
                          <div>{user.email}</div>
                          <div style={{ fontSize: '0.85rem', color: 'var(--muted)' }}>
                            {user.phoneNumber || '–'}
                          </div>
                        </td>
                        <td>{roleBadge(user.roleName)}</td>
                        <td>
                          <select
                            value={user.roleId}
                            disabled={busy}
                            style={{ padding: '0.3rem 0.6rem', fontSize: '0.88rem' }}
                            onChange={(e) => handleRoleChange(user.userId, e.target.value)}
                          >
                            {roles.length === 0 ? (
                              <option value={user.roleId}>{user.roleName}</option>
                            ) : (
                              roles.map((r) => (
                                <option key={r.roleId} value={r.roleId}>
                                  {r.roleName === 'Customer' ? 'Khách hàng'
                                    : r.roleName === 'Receptionist' ? 'Lễ tân (Receptionist)'
                                    : r.roleName === 'Manager' ? 'Quản lý (Manager)'
                                    : 'Quản trị viên (Admin)'}
                                </option>
                              ))
                            )}
                          </select>
                        </td>
                        <td>
                          {user.isActive ? (
                            <span className="pill ok">Đang hoạt động</span>
                          ) : (
                            <span className="pill" style={{ background: '#9d3b32', color: 'white' }}>Đã khóa</span>
                          )}
                        </td>
                        <td style={{ textAlign: 'right' }}>
                          <button
                            className="btn small secondary"
                            type="button"
                            onClick={() => toggle(user.userId)}
                          >
                            {user.isActive ? 'Khóa tài khoản' : 'Mở khóa'}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          );
        }}
      </AsyncView>
    </div>
  );
}
