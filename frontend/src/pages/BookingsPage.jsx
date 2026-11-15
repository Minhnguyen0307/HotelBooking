import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, BookingTable, useLoad } from '../ui';
import { dateTime, money, statusLabel } from '../format';

function paymentMethodLabel(item) {
  const method = (item.paymentMethod || '').toLowerCase();
  if (method === 'cash') return 'Tiền mặt khi nhận phòng';
  if (method.includes('vietqr')) return 'VietQR mô phỏng';
  if (method === 'now') return 'VietQR mô phỏng';
  return item.paymentMethod || 'Chưa chọn';
}

function paymentStatusLabel(item) {
  if (item.paymentStatus === 'Refunded') return 'Đã hoàn tiền';
  if (item.paymentStatus === 'Success') return 'Đã thanh toán';
  if (item.paymentStatus === 'Pending') return 'Chưa thanh toán';
  if (item.paymentStatus === 'Failed') return 'Thanh toán thất bại';
  return 'Chưa ghi nhận thanh toán';
}

function BookingDetails({ item }) {
  const paidAmount = Math.min(item.totalPrice || 0, item.paidAmount || 0);
  const remainingAmount = Math.max(0, (item.totalPrice || 0) - paidAmount);
  const paymentStateClass = item.paymentStatus === 'Success' || item.paymentStatus === 'Refunded'
    ? 'paid'
    : item.paymentStatus === 'Failed' ? 'failed' : 'unpaid';

  return (
    <section className="booking-detail-panel" aria-label={`Chi tiết đơn ${item.bookingId}`}>
      <div className="booking-detail-heading">
        <div>
          <p className="eyebrow">Thông tin đặt phòng</p>
          <h2>Đơn #{item.bookingId}</h2>
        </div>
        <span className="pill">{statusLabel(item.status)}</span>
      </div>
      <div className="booking-detail-grid">
        <div className="booking-detail-field">
          <span>Khách sạn và phòng</span>
          <strong>{item.propertyName ? `${item.propertyName} · ` : ''}{item.roomNumber} · {item.typeName}</strong>
        </div>
        <div className="booking-detail-field">
          <span>Ngày giờ đặt</span>
          <strong>{dateTime(item.createdAt)}</strong>
        </div>
        <div className="booking-detail-field">
          <span>Nhận phòng</span>
          <strong>{item.checkInDate} · 14:00</strong>
        </div>
        <div className="booking-detail-field">
          <span>Trả phòng</span>
          <strong>{item.checkOutDate} · 12:00</strong>
        </div>
        <div className="booking-detail-field">
          <span>Số khách</span>
          <strong>{item.adultCount || 0} người lớn · {item.childCount || 0} trẻ em</strong>
        </div>
        <div className="booking-detail-field">
          <span>Người lưu trú</span>
          <strong>{item.guestName || item.customerName || '—'}{item.guestPhone ? ` · ${item.guestPhone}` : ''}</strong>
        </div>
        <div className="booking-detail-field">
          <span>Hình thức thanh toán</span>
          <strong>{paymentMethodLabel(item)}</strong>
        </div>
        <div className="booking-detail-field">
          <span>Tình trạng thanh toán</span>
          <strong className={`payment-state ${paymentStateClass}`}>{paymentStatusLabel(item)}</strong>
        </div>
        <div className="booking-detail-field">
          <span>Đã thanh toán</span>
          <strong>{money(paidAmount)}</strong>
        </div>
        <div className="booking-detail-field">
          <span>Còn phải thanh toán</span>
          <strong>{money(remainingAmount)}</strong>
        </div>
        {item.paymentDate && (
          <div className="booking-detail-field">
            <span>Thời điểm thanh toán</span>
            <strong>{dateTime(item.paymentDate)}</strong>
          </div>
        )}
        {item.refundStatus && (
          <div className="booking-detail-field">
            <span>Hoàn tiền</span>
            <strong>{item.refundStatus === 'Pending' ? 'Đang chờ xử lý (mô phỏng)' : item.refundStatus === 'Completed' ? 'Đã hoàn tất (mô phỏng)' : item.refundStatus}</strong>
          </div>
        )}
        {item.guestNote && (
          <div className="booking-detail-field full">
            <span>Ghi chú</span>
            <strong>{item.guestNote}</strong>
          </div>
        )}
        {item.cancelReason && (
          <div className="booking-detail-field full">
            <span>Lý do hủy</span>
            <strong>{item.cancelReason}</strong>
          </div>
        )}
        <div className="booking-detail-total">
          <span>Tổng đơn</span>
          <strong>{money(item.totalPrice)}</strong>
        </div>
      </div>
    </section>
  );
}

export function BookingsPage() {
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [cancelBookingId, setCancelBookingId] = useState(null);
  const [cancelReason, setCancelReason] = useState('');
  const [savingCancel, setSavingCancel] = useState(false);
  const [expandedBookingId, setExpandedBookingId] = useState(null);
  const state = useLoad(() => api('/api/bookings'), [tick]);

  async function cancel(event, bookingId) {
    event.preventDefault();
    setError('');
    setNotice('');
    setSavingCancel(true);
    try {
      const result = await api(`/api/bookings/${bookingId}/cancel`, {
        method: 'POST',
        body: { reason: cancelReason }
      });
      setNotice(result.message);
      setCancelBookingId(null);
      setCancelReason('');
      setTick((value) => value + 1);
    } catch (err) {
      setError(err.message);
    } finally {
      setSavingCancel(false);
    }
  }

  return (
    <AsyncView state={state}>
      {(bookings) => (
        <>
          <h1>Lịch sử đặt phòng</h1>
          {error && <div className="banner">{error}</div>}
          {notice && <div className="banner ok" role="status">{notice}</div>}
          <BookingTable
            rows={bookings}
            showPaymentChoice={false}
            expandedId={expandedBookingId}
            onToggleDetails={(bookingId) => setExpandedBookingId((current) => current === bookingId ? null : bookingId)}
            renderDetails={(item) => <BookingDetails item={item} />}
            actions={(item) => (
              <div className="stack">
                {item.status === 'Pending' && <Link className="btn small" to={`/pay/${item.bookingId}`}>Thanh toán</Link>}
                {item.canCustomerCancel && cancelBookingId !== item.bookingId && (
                  <button
                    className="btn small danger"
                    type="button"
                    onClick={() => {
                      setCancelBookingId(item.bookingId);
                      setCancelReason('');
                      setError('');
                    }}
                  >
                    Hủy
                  </button>
                )}
                {cancelBookingId === item.bookingId && (
                  <form className="stack" onSubmit={(event) => cancel(event, item.bookingId)}>
                    <label>
                      Lý do hủy
                      <textarea
                        value={cancelReason}
                        onChange={(event) => setCancelReason(event.target.value)}
                        maxLength={300}
                        rows={3}
                        required
                        placeholder="Cho biết lý do bạn muốn hủy đơn"
                      />
                    </label>
                    <p className="muted">Chỉ hủy được trước 00:00 ngày nhận phòng.</p>
                    <p className="banner info">
                      {item.refundAmount > 0
                        ? `Đã thanh toán ${money(item.refundAmount)}. Khi hủy, hệ thống sẽ ghi nhận yêu cầu hoàn tiền (mô phỏng), dự kiến 3–5 ngày làm việc.`
                        : 'Đơn chưa thanh toán thành công nên sẽ không phát sinh hoàn tiền.'}
                    </p>
                    <div className="row">
                      <button className="btn small danger" type="submit" disabled={!cancelReason.trim() || savingCancel}>
                        {savingCancel ? 'Đang xử lý…' : 'Xác nhận hủy'}
                      </button>
                      <button className="btn small secondary" type="button" disabled={savingCancel} onClick={() => setCancelBookingId(null)}>Đóng</button>
                    </div>
                  </form>
                )}
              </div>
            )}
          />
        </>
      )}
    </AsyncView>
  );
}
