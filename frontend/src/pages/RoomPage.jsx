import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';
import { money, today, tomorrow } from '../format';
import { AsyncView, Photo, StarPicker, StarRow, StayDates, useLoad } from '../ui';

export function RoomPage() {
  const { id } = useParams();
  const { user, hasRole } = useAuth();
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [preview, setPreview] = useState(null);
  const [error, setError] = useState('');
  const saved = JSON.parse(sessionStorage.getItem('search') || '{}');
  const state = useLoad(() => api(`/api/rooms/${id}`), [id, tick]);

  async function onPreview(event) {
    const form = event.target.form;
    const data = Object.fromEntries(new FormData(form));
    setError('');
    try {
      const adults = Number(data.adults || 1);
      const children = Number(data.children || 0);
      const totalGuests = adults + children;
      const result = await api(`/api/bookings/preview?roomId=${id}&checkInDate=${data.checkInDate}&checkOutDate=${data.checkOutDate}&numberOfGuests=${totalGuests}&adults=${adults}&children=${children}`);
      setPreview(result);
    } catch (err) {
      setError(err.message);
    }
  }

  async function onBook(event) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const adults = Number(data.adults || 1);
      const children = Number(data.children || 0);
      const totalGuests = adults + children;
      const result = await api('/api/bookings', {
        method: 'POST',
        body: {
          roomId: Number(id),
          checkInDate: data.checkInDate,
          checkOutDate: data.checkOutDate,
          numberOfGuests: totalGuests || Number(data.numberOfGuests || 1),
          adults,
          children,
          guestName: data.guestName,
          guestPhone: data.guestPhone,
          guestNote: data.guestNote,
          paymentChoice: data.paymentChoice
        }
      });
      if (result.requiresPayment) {
        navigate(`/pay/${result.bookingId}`, { state: { message: result.message, kind: 'ok' } });
      } else {
        navigate('/bookings', { state: { message: result.message, kind: 'ok' } });
      }
    } catch (err) {
      if (err.status === 401) navigate('/login', { state: { from: `/rooms/${id}` } });
      else setError(err.message);
    }
  }

  async function onReview(event) {
    event.preventDefault();
    setError('');
    try {
      const result = await api(`/api/rooms/${id}/reviews`, { method: 'POST', body: new FormData(event.target) });
      setTick((value) => value + 1);
      navigate(`/rooms/${id}`, { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  async function onReply(event, reviewId) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    try {
      const result = await api(`/api/rooms/reviews/${reviewId}/reply`, {
        method: 'POST',
        body: { response: data.response }
      });
      setTick((value) => value + 1);
      navigate(`/rooms/${id}`, { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(data) => {
        const room = data.room;
        const summary = room.reviewsSummary || { reviews: [], averageRating: 0, totalReviews: 0 };
        return (
          <>
            {error && <div className="banner">{error}</div>}
            <div className="split">
              <article className="card">
                <Photo url={(room.imageUrls || [])[0]} />
                <div className="pad">
                  <p className="pill">{room.propertyName} · {room.area}</p>
                  <h1>Phòng {room.roomNumber}</h1>
                  <p>{room.description}</p>
                  <p className="muted">{room.address} · {room.typeName}</p>
                  <p>{(room.amenities || []).map((item) => <span className="pill" key={item}>{item}</span>)}</p>
                  <p className="price">{money(room.basePrice)} / đêm</p>
                  <p className="fare">{room.occupancyRule}</p>
                </div>
              </article>
              <form className="panel stack" onSubmit={onBook}>
                <h2>Mời bạn lưu trú</h2>
                <StayDates checkIn={saved.checkInDate || today()} checkOut={saved.checkOutDate || tomorrow()} />
                <div className="form-grid">
                  <label>
                    Người lớn
                    <input name="adults" type="number" min="1" defaultValue={saved.adults || 1} onChange={() => setPreview(null)} required />
                  </label>
                  <label>
                    Trẻ em
                    <input name="children" type="number" min="0" defaultValue={saved.children || 0} onChange={() => setPreview(null)} required />
                  </label>
                </div>
                <label>Người nhận phòng<input name="guestName" defaultValue={user?.fullName || ''} required /></label>
                <label>Số điện thoại<input name="guestPhone" inputMode="tel" pattern="0[0-9]{9}" defaultValue={user?.phoneNumber || ''} required /></label>
                <label>Lời nhắn<textarea name="guestNote" maxLength="500" placeholder="Giờ đến, yêu cầu phòng..."></textarea></label>
                <fieldset className="pay-choice">
                  <legend>Thanh toán</legend>
                  <label className="choice"><input type="radio" name="paymentChoice" value="Now" defaultChecked /> Thanh toán ngay</label>
                  <label className="choice"><input type="radio" name="paymentChoice" value="Cash" /> Tiền mặt khi nhận phòng</label>
                </fieldset>
                {preview ? (
                  <div className="panel stack" style={{ background: 'var(--paper)', fontSize: '0.9rem', padding: '0.8rem' }}>
                    <div className="spread">
                      <span>Thời gian:</span>
                      <strong>{preview.nights} đêm · {preview.numberOfGuests} khách</strong>
                    </div>
                    <div className="spread">
                      <span>Giá gốc/đêm:</span>
                      <span>{money(preview.pricePerNight)}</span>
                    </div>
                    {preview.weekendSurcharge > 0 && (
                      <div className="spread" style={{ color: 'var(--sand)' }}>
                        <span>Phụ thu cuối tuần / lễ:</span>
                        <span>+{money(preview.weekendSurcharge)}</span>
                      </div>
                    )}
                    {preview.extraGuestSurcharge > 0 && (
                      <div className="spread" style={{ color: 'var(--sand)' }}>
                        <span>Phụ thu người phát sinh:</span>
                        <span>+{money(preview.extraGuestSurcharge)}</span>
                      </div>
                    )}
                    <hr style={{ border: 'none', borderTop: '1px solid var(--line)', margin: '0.3rem 0' }} />
                    <div className="spread" style={{ fontSize: '1.1rem' }}>
                      <strong>Tổng tiền:</strong>
                      <span className="price">{money(preview.totalPrice)}</span>
                    </div>
                    {preview.summary && <small className="muted">{preview.summary}</small>}
                    {preview.priceBreakdown && <small className="muted">{preview.priceBreakdown}</small>}
                  </div>
                ) : (
                  <div className="muted" style={{ fontSize: '0.88rem' }}>
                    💡 Bấm "Xem chi tiết giá" để tính tiền theo ngày thường / cuối tuần & phụ thu khách.
                  </div>
                )}
                <button className="btn secondary" type="button" onClick={onPreview}>Xem chi tiết giá</button>
                <button className="btn" type="submit">Xác nhận đặt phòng</button>
              </form>
            </div>
            <section>
              <h2>Đánh giá <StarRow value={summary.averageRating} /> · {summary.totalReviews || 0} lượt</h2>
              {(summary.reviews || []).length === 0 && <p className="muted">Chưa có đánh giá.</p>}
              {(summary.reviews || []).map((review) => (
                <article className="panel" key={review.reviewId}>
                  <div className="spread">
                    <strong>{review.userName}</strong>
                    <StarRow value={review.rating} />
                  </div>
                  <p>{review.comment}</p>
                  {review.staffResponse && <p className="muted">Phản hồi: {review.staffResponse}</p>}
                  {hasRole('Receptionist', 'Manager', 'Admin') && (
                    <form className="row" onSubmit={(event) => onReply(event, review.reviewId)}>
                      <input name="response" placeholder="Phản hồi của khách sạn" required />
                      <button className="btn small" type="submit">Gửi</button>
                    </form>
                  )}
                </article>
              ))}
              {user ? (
                data.hasStayed ? (
                  <form className="panel stack" onSubmit={onReview}>
                    <h3>Viết đánh giá</h3>
                    <p className="pill ok" style={{ alignSelf: 'flex-start' }}>✓ Bạn đã hoàn tất kỳ lưu trú tại phòng này</p>
                    <StarPicker />
                    <label>Nhận xét<textarea name="comment" placeholder="Chia sẻ trải nghiệm của bạn về phòng này..." required></textarea></label>
                    <label>Ảnh đính kèm<input name="images" type="file" accept="image/*" multiple /></label>
                    <button className="btn" type="submit">Gửi đánh giá</button>
                  </form>
                ) : (
                  <div className="panel" style={{ background: '#fdf9f3', border: '1px dashed #d7c2a5' }}>
                    <p style={{ margin: 0, color: 'var(--muted)' }}>
                      🔒 <strong>Chính sách đánh giá:</strong> Bạn chỉ có thể đánh giá và chấm điểm phòng sau khi đã hoàn tất lưu trú (Check-out) tại phòng này.
                    </p>
                  </div>
                )
              ) : <p><Link to="/login">Đăng nhập</Link> để đánh giá phòng.</p>}
            </section>
          </>
        );
      }}
    </AsyncView>
  );
}
