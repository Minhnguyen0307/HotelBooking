import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, BookingTable, useLoad } from '../ui';

export function BookingsPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/bookings'), [tick]);

  async function cancel(bookingId) {
    const reason = window.prompt('Lý do hủy', '') || '';
    setError('');
    try {
      const result = await api(`/api/bookings/${bookingId}/cancel`, { method: 'POST', body: { reason } });
      setTick((value) => value + 1);
      navigate('/bookings', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(bookings) => (
        <>
          <h1>Đơn của tôi</h1>
          {error && <div className="banner">{error}</div>}
          <BookingTable
            rows={bookings}
            actions={(item) => (
              <div className="row">
                {item.status === 'Pending' && <Link className="btn small" to={`/pay/${item.bookingId}`}>Thanh toán</Link>}
                {['Pending', 'Confirmed'].includes(item.status) && (
                  <button className="btn small danger" type="button" onClick={() => cancel(item.bookingId)}>Hủy</button>
                )}
              </div>
            )}
          />
        </>
      )}
    </AsyncView>
  );
}
