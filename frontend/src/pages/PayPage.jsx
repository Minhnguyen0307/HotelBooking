import { useEffect } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api';
import { money } from '../format';
import { LuxBar } from '../lux';
import { AsyncView, useLoad } from '../ui';

export function PayPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const state = useLoad(() => api(`/api/payments/${id}`), [id]);

  useEffect(() => {
    const timer = window.setInterval(() => state.retry(), 5000);
    return () => window.clearInterval(timer);
  }, [id]);

  const paid = state.data?.alreadyPaid || state.data?.booking?.status === 'Confirmed';
  useEffect(() => {
    if (paid) navigate('/bookings', { replace: true, state: { message: `Đơn #${id} đã được nhân viên xác nhận thanh toán.`, kind: 'ok' } });
  }, [paid, id, navigate]);

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
          <section className="lux-book-page lux-book-card lux-reserve real-payment">
            <p className="lux-kicker dark">HBS Reservation</p>
            <h1>Thanh toán đơn #{booking.bookingId}</h1>
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
            <div className="qr-transfer-panel">
              {data.paymentQrUrl && <img src={data.paymentQrUrl} alt={`VietQR thanh toán đơn ${booking.bookingId}`} />}
              <div>
                <strong>Quét VietQR bằng ứng dụng ngân hàng</strong>
                <p>Nội dung chuyển khoản: <b>BOOKING_{booking.bookingId}</b></p>
                {data.beneficiary && <p>{data.beneficiary.bankId} · {data.beneficiary.accountNumber}<br />{data.beneficiary.accountName}</p>}
                <p>Số tiền: <b>{money(booking.amountDue)}</b></p>
                <p className="muted">Trang sẽ tự chuyển về đơn của bạn sau khi nhân viên kiểm tra giao dịch vào tài khoản và xác nhận.</p>
              </div>
            </div>
            {!data.bankConfigured && <div className="banner">Chưa cấu hình tài khoản nhận VietQR. Hãy kiểm tra cấu hình VietQR của máy chủ.</div>}
            <p role="status" className="lux-note">Đang chờ xác nhận thanh toán… Trang kiểm tra trạng thái tự động mỗi 5 giây.</p>
            <Link to="/bookings" className="lux-line">Quay lại đơn của tôi</Link>
          </section>
        );
      }}
      </AsyncView>
    </div>
  );
}
