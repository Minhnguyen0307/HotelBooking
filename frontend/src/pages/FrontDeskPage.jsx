import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, BookingTable, StarRow, useLoad } from '../ui';

export function FrontDeskPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/frontdesk'), [tick]);

  async function act(path, bookingId) {
    setError('');
    try {
      const result = await api(`/api/frontdesk/${path}/${bookingId}`, { method: 'POST' });
      setTick((value) => value + 1);
      navigate('/frontdesk', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  async function onReply(event, reviewId) {
    event.preventDefault();
    const data = Object.fromEntries(new FormData(event.target));
    setError('');
    try {
      const result = await api(`/api/rooms/reviews/${reviewId}/reply`, {
        method: 'POST',
        body: { response: data.response }
      });
      setTick((value) => value + 1);
      navigate('/frontdesk', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  function block(title, rows, path, label) {
    return (
      <>
        <h2>{title}</h2>
        <BookingTable
          rows={rows}
          actions={(item) => (
            <button className="btn small" type="button" onClick={() => act(path, item.bookingId)}>{label}</button>
          )}
        />
      </>
    );
  }

  return (
    <AsyncView state={state}>
      {(data) => (
        <>
          <h1>Lễ tân</h1>
          {error && <div className="banner">{error}</div>}
          {block('Khách đến hôm nay', data.arrivals, 'check-in', 'Check-in')}
          {block('Khách đi hôm nay', data.departures, 'check-out', 'Check-out')}
          {block('Yêu cầu hủy', data.cancelRequests, 'confirm-cancel', 'Xác nhận hủy')}
          <h2>Đang ở</h2>
          <BookingTable
            rows={data.checkedIn}
            actions={(item) => (
              <button className="btn small" type="button" onClick={() => act('check-out', item.bookingId)}>Check-out</button>
            )}
          />
          <h2>Đánh giá</h2>
          {(data.reviews || []).length === 0 && <p className="muted">Chưa có đánh giá.</p>}
          {(data.reviews || []).map((review) => (
            <article className="panel" key={review.reviewId}>
              <strong>Phòng {review.roomNumber} · {review.userName}</strong>
              <StarRow value={review.rating} />
              <p>{review.comment}</p>
              {review.staffResponse ? <p className="muted">{review.staffResponse}</p> : (
                <form className="row" onSubmit={(event) => onReply(event, review.reviewId)}>
                  <input name="response" placeholder="Phản hồi" required />
                  <button className="btn small" type="submit">Gửi</button>
                </form>
              )}
            </article>
          ))}
        </>
      )}
    </AsyncView>
  );
}
