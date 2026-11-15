import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, useLoad } from '../ui';

export function CustomersPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/admin/customers'), [tick]);

  async function toggle(id) {
    setError('');
    try {
      const result = await api(`/api/admin/customers/${id}/toggle`, { method: 'POST' });
      setTick((value) => value + 1);
      navigate('/admin/customers', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(customers) => (
        <>
          <h1>Khách hàng</h1>
          {error && <div className="banner">{error}</div>}
          <div className="table-wrap">
            <table>
              <thead><tr><th>Tên</th><th>Email</th><th>Điện thoại</th><th>Trạng thái</th><th></th></tr></thead>
              <tbody>
                {customers.map((customer) => (
                  <tr key={customer.userId}>
                    <td>{customer.fullName}</td>
                    <td>{customer.email}</td>
                    <td>{customer.phoneNumber || ''}</td>
                    <td>{customer.isActive ? 'Đang hoạt động' : 'Đã khóa'}</td>
                    <td>
                      <button className="btn small" type="button" onClick={() => toggle(customer.userId)}>
                        {customer.isActive ? 'Khóa' : 'Mở'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </AsyncView>
  );
}
