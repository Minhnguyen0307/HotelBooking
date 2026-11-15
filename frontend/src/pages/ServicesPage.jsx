import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, useLoad } from '../ui';

export function ServicesPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/admin/services'), [tick]);

  async function save(event, code) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api(`/api/admin/services/${code}`, {
        method: 'PUT',
        body: { name: data.name, price: Number(data.price) }
      });
      setTick((value) => value + 1);
      navigate('/admin/services', { state: { message: result.message, kind: 'ok' }, replace: true });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(services) => (
        <>
          <header className="page-head">
            <p className="eyebrow">Quản trị</p>
            <h1>Giá dịch vụ</h1>
          </header>
          {error && <div className="banner">{error}</div>}
          <div className="table-wrap">
            <table>
              <thead><tr><th>Dịch vụ</th><th>Cách tính</th><th>Dùng ở</th><th>Giá</th><th></th></tr></thead>
              <tbody>
                {services.map((item) => (
                  <tr key={item.serviceCode}>
                    <td><input form={`svc-${item.serviceCode}`} name="name" defaultValue={item.name} required /></td>
                    <td>{item.unitLabel}</td>
                    <td>{item.forBooking && item.forDesk ? 'Đặt phòng và lễ tân' : item.forBooking ? 'Đặt phòng' : 'Lễ tân'}</td>
                    <td><input form={`svc-${item.serviceCode}`} name="price" type="number" min="0" defaultValue={item.price} required /></td>
                    <td><button className="btn small" form={`svc-${item.serviceCode}`} type="submit">Lưu</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {services.map((item) => (
            <form id={`svc-${item.serviceCode}`} key={item.serviceCode} onSubmit={(event) => save(event, item.serviceCode)} />
          ))}
        </>
      )}
    </AsyncView>
  );
}
