import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import QRCode from 'qrcode';
import { api } from '../api';
import { AsyncView, BookingTable, useLoad } from '../ui';

const INVOICE_STATUSES = ['CheckedOut', 'Confirmed', 'CheckedIn'];

export function BookingsPage() {
  const navigate = useNavigate();
  const [tick, setTick] = useState(0);
  const [error, setError] = useState('');
  const [qrBooking, setQrBooking] = useState(null);
  const state = useLoad(() => api('/api/bookings'), [tick]);

  async function cancel(bookingId) {
    const reason = window.prompt('Lý do hủy', '') || '';
    setError('');
    try {
      const result = await api(`/api/bookings/${bookingId}/cancel`, { method: 'POST', body: { reason } });
      setTick((value) => value + 1);
      navigate('/bookings', { state: { message: result.message, kind: 'ok' } });
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <AsyncView state={state}>
      {(bookings) => (
        <>
          <h1>Đơn của tôi</h1>
          {error && <div className="banner">{error}</div>}
          <BookingTable
            rows={bookings}
            actions={(item) => (
              <div className="row" style={{ gap: '6px' }}>
                <button
                  className="btn small"
                  type="button"
                  style={{ background: '#0070f3', color: '#fff', border: 'none' }}
                  onClick={() => setQrBooking(item)}
                >
                  📱 Mã QR
                </button>
                {item.status === 'Pending' && <Link className="btn small" to={`/pay/${item.bookingId}`}>Thanh toán</Link>}
                {['Pending', 'Confirmed'].includes(item.status) && (
                  <button className="btn small danger" type="button" onClick={() => cancel(item.bookingId)}>Hủy</button>
                )}
                {INVOICE_STATUSES.includes(item.status) && (
                  <Link className="btn small secondary" to={`/invoice/${item.bookingId}`}>🖨 Hóa đơn</Link>
                )}
              </div>
            )}
          />
          {qrBooking && <QrModal booking={qrBooking} onClose={() => setQrBooking(null)} />}
        </>
      )}
    </AsyncView>
  );
}

function QrModal({ booking, onClose }) {
  const [qrDataUrl, setQrDataUrl] = useState('');

  useEffect(() => {
    // Generate QR code with fast checkin code format: HBS-{id}
    const payload = `HBS-${booking.bookingId}`;
    QRCode.toDataURL(payload, { width: 220, margin: 2 })
      .then((url) => setQrDataUrl(url))
      .catch((err) => console.error('Failed to generate QR', err));
  }, [booking]);

  return (
    <div style={{
      position: 'fixed',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      background: 'rgba(0,0,0,0.5)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 1000
    }}>
      <div style={{
        background: '#fff',
        borderRadius: '12px',
        padding: '24px',
        maxWidth: '380px',
        width: '90%',
        textAlign: 'center',
        boxShadow: '0 8px 30px rgba(0,0,0,0.2)'
      }}>
        <h3 style={{ margin: '0 0 8px 0', color: '#1a202c' }}>⚡ Fast Check-in QR Code</h3>
        <p style={{ margin: '0 0 16px 0', color: '#718096', fontSize: '0.88rem' }}>
          Đưa mã này cho lễ tân để nhận phòng tức thì trong 3 giây!
        </p>
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '14px' }}>
          {qrDataUrl ? (
            <img src={qrDataUrl} alt="Fast Check-in QR" style={{ width: '220px', height: '220px', border: '1px solid #e2e8f0', borderRadius: '8px' }} />
          ) : (
            <div style={{ width: '220px', height: '220px', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>Đang tạo QR...</div>
          )}
        </div>
        <div style={{ background: '#f7fafc', padding: '10px', borderRadius: '6px', marginBottom: '16px', textAlign: 'left', fontSize: '0.88rem' }}>
          <div><strong>Mã đơn:</strong> #{booking.bookingId} (Mã quét: <code>HBS-{booking.bookingId}</code>)</div>
          <div><strong>Phòng:</strong> {booking.roomNumber} ({booking.propertyName || 'HBS'})</div>
          <div><strong>Nhận phòng:</strong> {booking.checkInDate}</div>
        </div>
        <button
          className="btn"
          type="button"
          onClick={onClose}
          style={{ width: '100%', padding: '10px', borderRadius: '6px' }}
        >
          Đóng
        </button>
      </div>
    </div>
  );
}
