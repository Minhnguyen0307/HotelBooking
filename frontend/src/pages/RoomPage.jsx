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
      const result = await api(`/api/bookings/preview?roomId=${id}&checkInDate=${data.checkInDate}&checkOutDate=${data.checkOutDate}&numberOfGuests=${data.numberOfGuests}`);
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
      const result = await api('/api/bookings', {
        method: 'POST',
        body: {
          roomId: Number(id),
          checkInDate: data.checkInDate,
          checkOutDate: data.checkOutDate,
          numberOfGuests: Number(data.numberOfGuests)
        }
      });
      navigate(`/pay/${result.bookingId}`, { state: { message: result.message, kind: 'ok' } });
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
                <label>Số khách<input name="numberOfGuests" type="number" min="1" max={room.maxGuests} defaultValue={saved.guests || 1} required /></label>
                {preview && (
                  <div className="fare">
                    <span>{preview.stayLabel} · {preview.nights} đêm · {preview.numberOfGuests} khách</span>
                    <span>Giá phòng {money(preview.roomCharge)}</span>
                    <span>Phụ thu {money(preview.surcharge)}</span>
                    <strong>Tổng {money(preview.totalPrice)}</strong>
                    <span>{preview.summary}</span>
                  </div>
                )}
                <button className="btn secondary" type="button" onClick={onPreview}>Xem giá</button>
                <button className="btn" type="submit">Xác nhận đặt</button>
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
                <form className="panel stack" onSubmit={onReview}>
                  <h3>Chia sẻ trải nghiệm</h3>
                  <StarPicker />
                  <label>Nhận xét<textarea name="comment"></textarea></label>
                  <label>Ảnh<input name="images" type="file" accept="image/*" multiple /></label>
                  <button className="btn" type="submit">Gửi đánh giá</button>
                </form>
              ) : <p className="muted">Mời bạn <Link to="/login">đăng nhập</Link> để chia sẻ trải nghiệm.</p>}
            </section>
          </>
        );
      }}
    </AsyncView>
  );
}
