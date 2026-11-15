import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api';
import { useAuth } from '../auth';
import { money, today, tomorrow } from '../format';
import { AsyncView, Photo, StarPicker, StarRow, StayDates, useLoad } from '../ui';

function bookingPayload(data, roomId) {
  return {
    roomId: Number(roomId),
    checkInDate: data.checkInDate,
    checkOutDate: data.checkOutDate,
    adultCount: Number(data.adultCount || 0),
    childCount: Number(data.childCount || 0),
    numberOfGuests: Number(data.adultCount || 0) + Number(data.childCount || 0),
    guestName: data.guestName || '',
    guestPhone: data.guestPhone || '',
    guestNote: data.guestNote || '',
    paymentChoice: data.paymentChoice || 'Now',
    promoCode: data.promoCode || '',
    addOns: Object.entries(data)
      .filter(([key, value]) => key.startsWith('addon_') && Number(value) > 0)
      .map(([key, value]) => ({ code: key.slice('addon_'.length), quantity: Number(value) }))
  };
}

export function RoomPage() {
  const { id } = useParams();
  const { user, hasRole } = useAuth();
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [preview, setPreview] = useState(null);
  const [error, setError] = useState('');
  const saved = JSON.parse(sessionStorage.getItem('search') || '{}');
  const state = useLoad(async () => {
    const [detail, addOnsResult, promoResult] = await Promise.all([
      api(`/api/rooms/${id}`),
      api('/api/rooms/add-ons')
        .then((data) => ({ data }))
        .catch((error) => ({ error })),
      user
        ? api('/api/bookings/promos')
            .then((data) => ({ data }))
            .catch((error) => ({ error }))
        : Promise.resolve({ data: [] })
    ]);
    return {
      ...detail,
      addOns: addOnsResult.data || [],
      addOnsLoadError: addOnsResult.error,
      promoCodes: promoResult.data || [],
      promoLoadError: promoResult.error
    };
  }, [id, tick, user]);

  async function onPreview(event) {
    const form = event.target.form;
    const data = Object.fromEntries(new FormData(form));
    setError('');
    try {
      const result = await api('/api/bookings/preview', { method: 'POST', body: bookingPayload(data, id) });
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
          ...bookingPayload(data, id)
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
                <div className="form-grid guest-counts">
                  <label>Người lớn<input name="adultCount" type="number" min="1" max={room.maxGuests} defaultValue={saved.adults || saved.guests || 1} required /></label>
                  <label>Trẻ em<input name="childCount" type="number" min="0" max={room.maxGuests - 1} defaultValue={saved.children || 0} /></label>
                </div>
                <p className="muted">Tổng số người lớn và trẻ em dùng để kiểm tra sức chứa và phụ thu phòng.</p>
                {data.addOnsLoadError && <p className="muted">Dịch vụ cộng thêm tạm thời chưa tải được; bạn vẫn có thể xem giá phòng.</p>}
                <label>Người nhận phòng<input name="guestName" defaultValue={user?.fullName || ''} required /></label>
                <label>Số điện thoại<input name="guestPhone" inputMode="tel" pattern="0[0-9]{9}" defaultValue={user?.phoneNumber || ''} required /></label>
                <label>Lời nhắn<textarea name="guestNote" maxLength="500" placeholder="Giờ đến, yêu cầu phòng..."></textarea></label>
                <fieldset className="stack add-on-picker">
                  <legend>Dịch vụ cộng thêm</legend>
                  {(data.addOns || []).map((option) => (
                    <label className="add-on-option" key={option.code}>
                      <span><strong>{option.name}</strong><small>{option.description} {money(option.unitPrice)} / {option.unitLabel}</small></span>
                      <input name={`addon_${option.code}`} type="number" min="0" max={option.code === 'breakfast' ? '8' : option.maxQuantity} defaultValue="0" aria-label={`Số lượng ${option.name}`} />
                    </label>
                  ))}
                </fieldset>
                <label>Mã ưu đãi
                  <select name="promoCode" defaultValue="">
                    <option value="">Không áp dụng mã</option>
                    {(data.promoCodes || []).map((promo) => (
                      <option value={promo.code} key={promo.code}>
                        {promo.code} · {promo.discountPercent > 0 ? `giảm ${promo.discountPercent}%` : `giảm ${money(promo.discountAmount)}`} · đơn từ {money(promo.minimumOrderAmount)}{promo.isNewCustomerOnly ? ' · khách mới' : ''}
                      </option>
                    ))}
                  </select>
                </label>
                {!user && <p className="muted">Đăng nhập để xem các mã ưu đãi phù hợp với tài khoản.</p>}
                {user && data.promoLoadError && <p className="muted">Kho mã ưu đãi tạm thời chưa tải được; bạn vẫn có thể tiếp tục đặt phòng.</p>}
                {user && !data.promoLoadError && (data.promoCodes || []).length === 0 && <p className="muted">Hiện không có mã ưu đãi nào khả dụng cho tài khoản.</p>}
                <fieldset className="pay-choice">
                  <legend>Thanh toán</legend>
                  <label className="choice"><input type="radio" name="paymentChoice" value="Now" defaultChecked /> Thanh toán ngay</label>
                  <label className="choice"><input type="radio" name="paymentChoice" value="Cash" /> Tiền mặt khi nhận phòng</label>
                </fieldset>
                {preview && (
                  <div className="fare">
                    <span>{preview.stayLabel} · {preview.nights} đêm · {preview.adultCount} người lớn, {preview.childCount} trẻ em</span>
                    <span>Giá phòng {money(preview.roomCharge)}</span>
                    <span>Phụ thu {money(preview.surcharge)}</span>
                    {(preview.addOns || []).map((item) => <span key={item.code}>{item.name} · {item.quantity} {item.unitLabel}: {money(item.totalPrice)}</span>)}
                    {preview.discountTotal > 0 && <span>Ưu đãi {preview.promoCode}: −{money(preview.discountTotal)}</span>}
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
                  {(review.imageUrls || []).length > 0 && (
                    <div className="review-images">
                      {review.imageUrls.map((url) => <img key={url} src={url} alt="Ảnh khách gửi kèm đánh giá" loading="lazy" />)}
                    </div>
                  )}
                  {review.staffResponse && <p className="muted">Phản hồi: {review.staffResponse}</p>}
                  {hasRole('Receptionist', 'Manager', 'Admin') && (
                    <form className="row" onSubmit={(event) => onReply(event, review.reviewId)}>
                      <input name="response" placeholder="Phản hồi của khách sạn" required />
                      <button className="btn small" type="submit">Gửi</button>
                    </form>
                  )}
                </article>
              ))}
              {user && data.canReview ? (
                <form className="panel stack" onSubmit={onReview}>
                  <h3>Chia sẻ trải nghiệm sau check-out</h3>
                  <StarPicker />
                  <label>Nhận xét<textarea name="comment"></textarea></label>
                  <label>Ảnh<input name="images" type="file" accept="image/*" multiple /></label>
                  <button className="btn" type="submit">Gửi đánh giá</button>
                </form>
              ) : user ? <p className="muted">Chỉ khách đã hoàn tất check-out mới được gửi đánh giá. Mỗi khách chỉ đánh giá một lần cho mỗi phòng.</p> : <p className="muted">Mời bạn <Link to="/login">đăng nhập</Link>. Chỉ khách đã hoàn tất check-out mới được đánh giá.</p>}
            </section>
          </>
        );
      }}
    </AsyncView>
  );
}
