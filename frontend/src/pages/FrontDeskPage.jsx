import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { AsyncView, BookingTable, StarRow, useLoad } from '../ui';
import { today, addDays, money, statusLabel } from '../format';

// ─── Helpers ────────────────────────────────────────────────────────────────

function tileClass(status) {
  if (status === 'Available') return 'room-tile available';
  if (status === 'Booked' || status === 'Occupied') return 'room-tile booked';
  if (status === 'Maintenance') return 'room-tile maintenance';
  return 'room-tile other';
}

function statusIcon(status) {
  if (status === 'Available') return '✓';
  if (status === 'Booked' || status === 'Occupied') return '🛏';
  if (status === 'Maintenance') return '🔧';
  return '•';
}

// ─── Walk-in Booking Form ────────────────────────────────────────────────────

function WalkInForm({ matrixFloors, preselectedRoomId, onSuccess, onCancel }) {
  const availableRooms = (matrixFloors || []).flatMap(f =>
    f.rooms.filter(r => r.status === 'Available')
  );
  const [form, setForm] = useState({
    guestName: '',
    guestPhone: '',
    guestEmail: '',
    roomId: preselectedRoomId || (availableRooms[0]?.roomId ?? ''),
    checkInDate: today(),
    checkOutDate: addDays(today(), 1),
    adults: 1,
    children: 0,
    paymentMethod: 'Cash',
  });
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState('');

  function set(field) {
    return (e) => setForm(prev => ({ ...prev, [field]: e.target.value }));
  }

  async function submit(e) {
    e.preventDefault();
    setBusy(true);
    setErr('');
    try {
      const result = await api('/api/frontdesk/walk-in', {
        method: 'POST',
        body: {
          ...form,
          roomId: Number(form.roomId),
          adults: Number(form.adults),
          children: Number(form.children),
        },
      });
      onSuccess(result.message || 'Đặt phòng thành công!');
    } catch (ex) {
      setErr(ex.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="panel" onSubmit={submit} style={{ marginBottom: '1.5rem' }}>
      <h2 style={{ marginTop: 0 }}>Walk-in Booking</h2>
      {err && <div className="banner">{err}</div>}
      <div className="form-grid" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
        <label>
          Tên khách *
          <input value={form.guestName} onChange={set('guestName')} required />
        </label>
        <label>
          SĐT *
          <input type="tel" value={form.guestPhone} onChange={set('guestPhone')} required />
        </label>
        <label>
          Email
          <input type="email" value={form.guestEmail} onChange={set('guestEmail')} />
        </label>
        <label>
          Phòng số *
          <select value={form.roomId} onChange={set('roomId')} required>
            {availableRooms.length === 0 && <option value="">Không có phòng trống</option>}
            {availableRooms.map(r => (
              <option key={r.roomId} value={r.roomId}>
                {r.roomNumber} – {r.typeName} ({money(r.basePrice)}/đêm)
              </option>
            ))}
          </select>
        </label>
        <label>
          Nhận phòng *
          <input type="date" value={form.checkInDate} min={today()} onChange={set('checkInDate')} required />
        </label>
        <label>
          Trả phòng *
          <input type="date" value={form.checkOutDate} min={addDays(form.checkInDate, 1)} onChange={set('checkOutDate')} required />
        </label>
        <label>
          Người lớn
          <input type="number" value={form.adults} min={1} onChange={set('adults')} required />
        </label>
        <label>
          Trẻ em
          <input type="number" value={form.children} min={0} onChange={set('children')} required />
        </label>
        <label>
          Phương thức TT
          <select value={form.paymentMethod} onChange={set('paymentMethod')}>
            <option value="Cash">Tiền mặt</option>
            <option value="Card">Thẻ</option>
          </select>
        </label>
      </div>
      <div className="modal-actions">
        <button className="btn" type="submit" disabled={busy}>{busy ? 'Đang xử lý...' : '✚ Đặt phòng'}</button>
        <button className="btn secondary" type="button" onClick={onCancel}>Hủy</button>
      </div>
    </form>
  );
}

// ─── Room Tile Modal ─────────────────────────────────────────────────────────

function RoomModal({ room, onClose, onAction, matrixFloors }) {
  const [showTransfer, setShowTransfer] = useState(false);
  const [showExtend, setShowExtend] = useState(false);
  const [showInvoice, setShowInvoice] = useState(false);
  const [transferRoomId, setTransferRoomId] = useState('');
  const [newCheckOut, setNewCheckOut] = useState('');
  const [invoice, setInvoice] = useState(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState('');

  const availableRooms = (matrixFloors || []).flatMap(f =>
    f.rooms.filter(r => r.status === 'Available' && r.roomId !== room.roomId)
  );

  async function handleClean() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/rooms/${room.roomId}/clean`, { method: 'POST' });
      onAction(r.message || 'Cập nhật thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function handleCheckout() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/check-out/${room.activeBookingId}`, { method: 'POST' });
      onAction(r.message || 'Check-out thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function handleTransfer() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/bookings/${room.activeBookingId}/transfer-room`, {
        method: 'POST',
        body: { newRoomId: Number(transferRoomId) },
      });
      onAction(r.message || 'Đổi phòng thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function handleExtend() {
    setBusy(true);
    setErr('');
    try {
      const r = await api(`/api/frontdesk/bookings/${room.activeBookingId}/extend-stay`, {
        method: 'POST',
        body: { newCheckOutDate: newCheckOut },
      });
      onAction(r.message || 'Gia hạn thành công!');
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  async function loadInvoice() {
    setBusy(true);
    setErr('');
    try {
      const data = await api(`/api/frontdesk/bookings/${room.activeBookingId}/invoice`);
      setInvoice(data);
      setShowInvoice(true);
    } catch (ex) { setErr(ex.message); } finally { setBusy(false); }
  }

  const isBooked = room.status === 'Booked' || room.status === 'Occupied';
  const minExtendDate = room.checkOutDate ? addDays(room.checkOutDate, 1) : addDays(today(), 1);

  return (
    <div className="modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-box">
        {showInvoice && invoice ? (
          <>
            <style>{`@media print { .no-print { display:none!important } nav,footer,.topbar{display:none!important} body{background:white!important} }`}</style>
            <div className="invoice-container">
              <div className="invoice-header">
                <h1>HBS Luxury Hotel</h1>
                <p>123 Nguyễn Huệ, Q.1, TP.HCM &nbsp;|&nbsp; ☎ 1900-8888</p>
                <p><strong>HÓA ĐƠN #{invoice.bookingId}</strong> &nbsp;|&nbsp; {new Date().toLocaleDateString('vi-VN')}</p>
              </div>
              <table className="invoice-table">
                <tbody>
                  <tr><th>Khách</th><td>{invoice.guestName}</td></tr>
                  <tr><th>SĐT</th><td>{invoice.guestPhone}</td></tr>
                  <tr><th>Phòng</th><td>{invoice.roomNumber}</td></tr>
                  <tr><th>Nhận phòng</th><td>{invoice.checkInDate}</td></tr>
                  <tr><th>Trả phòng</th><td>{invoice.checkOutDate}</td></tr>
                </tbody>
              </table>
              <table className="invoice-table">
                <thead><tr><th>Khoản</th><th>Thành tiền</th></tr></thead>
                <tbody>
                  <tr><td>Tiền phòng cơ bản</td><td>{money(invoice.baseAmount)}</td></tr>
                  {invoice.extraGuestSurcharge > 0 && <tr><td>Phụ thu khách thêm</td><td>{money(invoice.extraGuestSurcharge)}</td></tr>}
                  {invoice.weekendSurcharge > 0 && <tr><td>Phụ thu cuối tuần</td><td>{money(invoice.weekendSurcharge)}</td></tr>}
                  <tr><td>VAT 10%</td><td>{money((invoice.totalAmount || 0) * 0.1)}</td></tr>
                </tbody>
              </table>
              <p className="invoice-total">Tổng cộng: {money(invoice.totalAmount)}</p>
              <div className="modal-actions no-print">
                <button className="btn" onClick={() => window.print()}>🖨 In hóa đơn</button>
                <button className="btn secondary" onClick={() => setShowInvoice(false)}>Đóng</button>
              </div>
            </div>
          </>
        ) : showTransfer ? (
          <>
            <h2>Đổi phòng</h2>
            {err && <div className="banner">{err}</div>}
            <label>Phòng mới
              <select value={transferRoomId} onChange={e => setTransferRoomId(e.target.value)}>
                <option value="">-- chọn phòng --</option>
                {availableRooms.map(r => (
                  <option key={r.roomId} value={r.roomId}>{r.roomNumber} – {r.typeName}</option>
                ))}
              </select>
            </label>
            <div className="modal-actions">
              <button className="btn" onClick={handleTransfer} disabled={!transferRoomId || busy}>Xác nhận</button>
              <button className="btn secondary" onClick={() => setShowTransfer(false)}>Hủy</button>
            </div>
          </>
        ) : showExtend ? (
          <>
            <h2>Gia hạn lưu trú</h2>
            {err && <div className="banner">{err}</div>}
            <label>Ngày trả phòng mới
              <input type="date" value={newCheckOut} min={minExtendDate}
                onChange={e => setNewCheckOut(e.target.value)} />
            </label>
            <div className="modal-actions">
              <button className="btn" onClick={handleExtend} disabled={!newCheckOut || busy}>Xác nhận</button>
              <button className="btn secondary" onClick={() => setShowExtend(false)}>Hủy</button>
            </div>
          </>
        ) : (
          <>
            <h2>Phòng {room.roomNumber}</h2>
            {err && <div className="banner">{err}</div>}
            <table style={{ width: '100%', marginBottom: '0.5rem' }}>
              <tbody>
                <tr><td style={{ color: 'var(--muted)', paddingRight: '1rem' }}>Hạng</td><td>{room.typeName}</td></tr>
                <tr><td style={{ color: 'var(--muted)' }}>Tình trạng</td><td>{statusLabel(room.status)}</td></tr>
                <tr><td style={{ color: 'var(--muted)' }}>Giá/đêm</td><td>{money(room.basePrice)}</td></tr>
                {isBooked && room.guestName && <>
                  <tr><td style={{ color: 'var(--muted)' }}>Khách</td><td>{room.guestName}</td></tr>
                  {room.guestPhone && <tr><td style={{ color: 'var(--muted)' }}>SĐT</td><td>{room.guestPhone}</td></tr>}
                  <tr><td style={{ color: 'var(--muted)' }}>Nhận phòng</td><td>{room.checkInDate}</td></tr>
                  <tr><td style={{ color: 'var(--muted)' }}>Trả phòng</td><td>{room.checkOutDate}</td></tr>
                  <tr><td style={{ color: 'var(--muted)' }}>Mã đặt phòng</td><td>#{room.activeBookingId}</td></tr>
                </>}
              </tbody>
            </table>
            <div className="modal-actions">
              {isBooked && <>
                <button className="btn" onClick={handleCheckout} disabled={busy}>Check-out</button>
                <button className="btn secondary" onClick={() => setShowTransfer(true)}>Đổi phòng</button>
                <button className="btn secondary" onClick={() => setShowExtend(true)}>Gia hạn</button>
                <button className="btn secondary" onClick={loadInvoice} disabled={busy}>In hóa đơn</button>
              </>}
              {room.status === 'Maintenance' && (
                <button className="btn" onClick={handleClean} disabled={busy}>✓ Dọn phòng xong</button>
              )}
              <button className="btn secondary" onClick={onClose}>Đóng</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

// ─── Room Matrix Tab ─────────────────────────────────────────────────────────

function RoomMatrixTab({ matrixState, onRoomClick }) {
  return (
    <AsyncView state={matrixState}>
      {(floors) => (
        <>
          <div className="matrix-legend">
            <span className="legend-item"><span className="legend-dot" style={{ background: '#1f6b45' }} />Trống</span>
            <span className="legend-item"><span className="legend-dot" style={{ background: '#9d3b32' }} />Có khách</span>
            <span className="legend-item"><span className="legend-dot" style={{ background: '#b5812f' }} />Bảo trì</span>
            <span className="legend-item"><span className="legend-dot" style={{ background: '#6d645b' }} />Khác</span>
          </div>
          {(floors || []).map(floor => (
            <div className="room-matrix-floor" key={floor.floor}>
              <h3>Tầng {floor.floor}</h3>
              <div className="room-tiles">
                {floor.rooms.map(room => (
                  <div
                    key={room.roomId}
                    className={tileClass(room.status)}
                    onClick={() => onRoomClick(room)}
                    title={`${room.roomNumber} – ${statusLabel(room.status)}`}
                  >
                    <strong>{room.roomNumber}</strong>
                    <small>{room.typeName}</small>
                    <div>{statusIcon(room.status)}</div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </>
      )}
    </AsyncView>
  );
}

// ─── Main Page ───────────────────────────────────────────────────────────────

export function FrontDeskPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');
  const [activeTab, setActiveTab] = useState('list');
  const [showWalkIn, setShowWalkIn] = useState(false);
  const [preselectedRoom, setPreselectedRoom] = useState(null);
  const [selectedRoom, setSelectedRoom] = useState(null);

  const listState = useLoad(() => api('/api/frontdesk'), [tick]);
  const matrixState = useLoad(() => api('/api/frontdesk/room-matrix'), [tick]);

  // Extract matrix floors for walk-in form available rooms
  const matrixFloors = matrixState.data ?? [];

  function refresh(msg) {
    setSuccessMsg(msg || '');
    setSelectedRoom(null);
    setShowWalkIn(false);
    setPreselectedRoom(null);
    setTick(t => t + 1);
  }

  async function act(path, bookingId) {
    setError('');
    try {
      const result = await api(`/api/frontdesk/${path}/${bookingId}`, { method: 'POST' });
      setTick(t => t + 1);
      setSuccessMsg(result.message || 'Thành công!');
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
        body: { response: data.response },
      });
      setTick(t => t + 1);
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

  function openWalkIn(room) {
    setPreselectedRoom(room?.roomId ?? null);
    setSelectedRoom(null);
    setShowWalkIn(true);
    setActiveTab('list');
  }

  return (
    <>
      <h1>Lễ tân</h1>

      {/* Tabs */}
      <div className="tabs">
        <button
          className={`tab-btn${activeTab === 'list' ? ' active' : ''}`}
          onClick={() => setActiveTab('list')}
        >
          📋 Danh sách
        </button>
        <button
          className={`tab-btn${activeTab === 'matrix' ? ' active' : ''}`}
          onClick={() => setActiveTab('matrix')}
        >
          🏨 Sơ đồ phòng
        </button>
      </div>

      {error && <div className="banner">{error}</div>}
      {successMsg && (
        <div className="banner" style={{ background: 'var(--ok)', color: 'white' }}>
          {successMsg}
        </div>
      )}

      {/* TAB 1 – Danh sách */}
      {activeTab === 'list' && (
        <AsyncView state={listState}>
          {(data) => (
            <>
              {/* Walk-in button */}
              <div style={{ marginBottom: '1rem' }}>
                <button
                  className="btn"
                  type="button"
                  onClick={() => { setShowWalkIn(s => !s); setPreselectedRoom(null); }}
                >
                  ✚ Walk-in Booking
                </button>
              </div>

              {showWalkIn && (
                <WalkInForm
                  matrixFloors={matrixFloors}
                  preselectedRoomId={preselectedRoom}
                  onSuccess={refresh}
                  onCancel={() => setShowWalkIn(false)}
                />
              )}

              {block('Khách đến hôm nay', data.arrivals, 'check-in', 'Check-in')}
              {block('Khách đi hôm nay', data.departures, 'check-out', 'Check-out')}
              {block('Yêu cầu hủy', data.cancelRequests, 'confirm-cancel', 'Xác nhận hủy')}

              <h2>Đang ở</h2>
              <BookingTable
                rows={data.checkedIn}
                actions={(item) => (
                  <button className="btn small" type="button" onClick={() => act('check-out', item.bookingId)}>
                    Check-out
                  </button>
                )}
              />

              <h2>Đánh giá</h2>
              {(data.reviews || []).length === 0 && <p className="muted">Chưa có đánh giá.</p>}
              {(data.reviews || []).map((review) => (
                <article className="panel" key={review.reviewId}>
                  <strong>Phòng {review.roomNumber} · {review.userName}</strong>
                  <StarRow value={review.rating} />
                  <p>{review.comment}</p>
                  {review.staffResponse ? (
                    <p className="muted">{review.staffResponse}</p>
                  ) : (
                    <form className="row" onSubmit={(e) => onReply(e, review.reviewId)}>
                      <input name="response" placeholder="Phản hồi" required />
                      <button className="btn small" type="submit">Gửi</button>
                    </form>
                  )}
                </article>
              ))}
            </>
          )}
        </AsyncView>
      )}

      {/* TAB 2 – Sơ đồ phòng */}
      {activeTab === 'matrix' && (
        <RoomMatrixTab
          matrixState={matrixState}
          onRoomClick={(room) => {
            if (room.status === 'Available') {
              openWalkIn(room);
            } else {
              setSelectedRoom(room);
            }
          }}
        />
      )}

      {/* Room detail modal */}
      {selectedRoom && (
        <RoomModal
          room={selectedRoom}
          matrixFloors={matrixFloors}
          onClose={() => setSelectedRoom(null)}
          onAction={refresh}
        />
      )}
    </>
  );
}
