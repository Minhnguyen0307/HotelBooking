import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api';
import { money } from '../format';
import { LuxBar } from '../lux';
import { AsyncView, useLoad } from '../ui';

export function PayPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const state = useLoad(() => api(`/api/payments/${id}`), [id]);

  async function onSubmit(event) {
    event.preventDefault();
    setError('');
    try {
      const result = await api('/api/payments/process', {
        method: 'POST',
        body: { bookingId: Number(id) }
      });
      navigate('/bookings', {
        state: { message: `${result.message} Mã giao dịch ${result.transactionId}`, kind: 'ok' }
      });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="lux lux-page">
      <LuxBar solid />
      <AsyncView state={state}>
      {(data) => {
        const booking = data.booking;
        if (data.alreadyPaid || booking.status !== 'Pending') {
          return (
            <section className="lux-book-page">
              <article className="lux-book-card lux-reserve">
                <h1>Đơn #{booking.bookingId}</h1>
                <p>Đơn này không còn chờ thanh toán.</p>
                <Link className="lux-gold" to="/bookings">Về đơn của tôi</Link>
              </article>
            </section>
          );
        }
        return (
          <form className="lux-book-page lux-book-card lux-reserve" onSubmit={onSubmit}>
            <p className="lux-kicker dark">HBS Reservation</p>
            <h1>Thanh toán đơn #{booking.bookingId}</h1>
            {error && <div className="banner">{error}</div>}
            <p>{booking.guestName} · {booking.guestPhone}</p>
            {booking.guestNote && <p className="muted">{booking.guestNote}</p>}
            <p>{booking.checkInDate} → {booking.checkOutDate} · {booking.numberOfGuests} khách</p>
            <div className="lux-quote">
              {booking.stayLabel && <span>{booking.stayLabel}</span>}
              {booking.roomCharge != null && <span>Giá phòng {money(booking.roomCharge)}</span>}
              {booking.surcharge > 0 && <span>Phụ thu {money(booking.surcharge)}</span>}
              {booking.summary && <span>{booking.summary}</span>}
              {(booking.services || []).map((line) => (
                <span key={line.name}>{line.name} {money(line.amount)}</span>
              ))}
              {(booking.addOns || []).map((line, index) => (
                <span key={`${line.description}-${index}`}>{line.description} {money(line.totalPrice)}</span>
              ))}
              {booking.discount > 0 && <span>Giảm giá −{money(booking.discount)}</span>}
              <strong>Tổng {money(booking.payable ?? booking.totalPrice)}</strong>
            </div>
            <button className="lux-gold" type="submit">Thanh toán thành công</button>
          </form>
        );
      }}
      </AsyncView>
    </div>
  );
}
