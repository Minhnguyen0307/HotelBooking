import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import QRCode from 'qrcode';
import { api } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

export function PayPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const [qrImage, setQrImage] = useState('');
  const state = useLoad(() => api(`/api/payments/${id}`), [id]);

  useEffect(() => {
    const amount = state.data?.booking?.amountDue;
    if (!amount) return;
    QRCode.toDataURL(`HBS-DEMO|BOOKING_${id}|AMOUNT_${amount}|NOT_A_REAL_PAYMENT`, {
      width: 240,
      margin: 1,
      errorCorrectionLevel: 'M'
    }).then(setQrImage).catch(() => setQrImage(''));
  }, [id, state.data?.booking?.amountDue]);

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
          <form className="panel stack account-card mock-payment" onSubmit={onSubmit}>
            <h1>Thanh toán #{booking.bookingId}</h1>
            {error && <div className="banner">{error}</div>}
            <p>{booking.guestName} · {booking.guestPhone}</p>
            {booking.guestNote && <p className="muted">{booking.guestNote}</p>}
            <p>{booking.checkInDate} → {booking.checkOutDate} · {booking.adultCount} người lớn, {booking.childCount} trẻ em</p>
            <div className="fare">
              {booking.stayLabel && <span>{booking.stayLabel}</span>}
              {booking.roomCharge != null && <span>Giá phòng {money(booking.roomCharge)}</span>}
              {booking.surcharge > 0 && <span>Phụ thu {money(booking.surcharge)}</span>}
              {(booking.addOns || []).map((item, index) => <span key={`${item.description}-${index}`}>{item.description}: {money(item.totalPrice)}</span>)}
              {booking.discount > 0 && <span>Ưu đãi −{money(booking.discount)}</span>}
              {booking.summary && <span>{booking.summary}</span>}
              <strong>Tổng {money(booking.amountDue)}</strong>
            </div>
            <div className="mock-qr-panel">
              {qrImage && <img src={qrImage} alt={`QR mô phỏng cho booking ${booking.bookingId}`} />}
              <div>
                <strong>Mã QR thanh toán demo</strong>
                <p>Nội dung: BOOKING_{booking.bookingId}</p>
                <p className="muted">Mã này chỉ chứa dữ liệu mô phỏng, không chuyển tiền và không kết nối ngân hàng.</p>
              </div>
            </div>
            <button className="btn" type="submit">Mô phỏng thanh toán thành công</button>
            <Link to="/bookings" className="btn secondary">Để thanh toán sau</Link>
          </form>
        );
      }}
    </AsyncView>
  );
}
