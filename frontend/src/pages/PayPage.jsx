import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

export function PayPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const state = useLoad(() => api(`/api/payments/${id}`), [id]);

  async function onSubmit(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api('/api/payments/process', {
        method: 'POST',
        body: {
          bookingId: Number(id),
          paymentMethod: data.paymentMethod,
          cardNumber: data.cardNumber
        }
      });
      navigate('/bookings', {
        state: { message: `${result.message} Mã giao dịch ${result.transactionId}`, kind: 'ok' }
      });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(data) => {
        const booking = data.booking;
        if (data.alreadyPaid || booking.status !== 'Pending') {
          return (
            <div className="panel">
              <h1>Đơn #{booking.bookingId}</h1>
              <p>Đơn này không còn chờ thanh toán.</p>
              <Link className="btn" to="/bookings">Về đơn của tôi</Link>
            </div>
          );
        }
        return (
          <form className="panel stack account-card" onSubmit={onSubmit}>
            <h1>Thanh toán #{booking.bookingId}</h1>
            {error && <div className="banner">{error}</div>}
            <p>{booking.guestName} · {booking.guestPhone}</p>
            {booking.guestNote && <p className="muted">{booking.guestNote}</p>}
            <p>{booking.checkInDate} → {booking.checkOutDate} · {booking.numberOfGuests} khách</p>
            <div className="fare">
              {booking.stayLabel && <span>{booking.stayLabel}</span>}
              {booking.roomCharge != null && <span>Giá phòng {money(booking.roomCharge)}</span>}
              {booking.surcharge > 0 && <span>Phụ thu {money(booking.surcharge)}</span>}
              {booking.summary && <span>{booking.summary}</span>}
              <strong>Tổng {money(booking.totalPrice)}</strong>
            </div>
            <label>
              Phương thức
              <select name="paymentMethod" defaultValue="Card">
                <option>Card</option>
                <option>Momo</option>
                <option>Bank</option>
              </select>
            </label>
            <label>Số thẻ hoặc tài khoản<input name="cardNumber" required /></label>
            <button className="btn" type="submit">Thanh toán</button>
          </form>
        );
      }}
    </AsyncView>
  );
}
