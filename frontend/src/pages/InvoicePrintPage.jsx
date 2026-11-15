import { useParams } from 'react-router-dom';
import { api } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

const printStyles = `
@media print {
  .no-print { display: none !important; }
  nav, footer, .topbar, .nav { display: none !important; }
  body { background: white !important; font-size: 12pt; }
  .invoice-container { box-shadow: none !important; border: none !important; }
}
`;

export function InvoicePrintPage() {
  const { id } = useParams();
  const state = useLoad(() => api(`/api/frontdesk/bookings/${id}/invoice`), [id]);

  return (
    <>
      <style>{printStyles}</style>
      <div style={{ padding: '2rem', background: '#fff', minHeight: '100vh' }}>
        <AsyncView state={state}>
          {(inv) => (
            <div className="invoice-container" style={{ maxWidth: 800, margin: '0 auto', padding: '2rem', background: '#fff', boxShadow: '0 4px 24px rgba(0,0,0,0.08)', borderRadius: 12 }}>
              {/* Header */}
              <div className="invoice-header">
                <h1>HBS Luxury Hotel</h1>
                <p style={{ margin: '0.2rem 0', color: '#555' }}>123 Nguyễn Huệ, Q.1, TP. Hồ Chí Minh</p>
                <p style={{ margin: '0.2rem 0', color: '#555' }}>☎ 1900-8888 &nbsp;|&nbsp; ✉ info@hbsluxury.vn</p>
                <p style={{ marginTop: '0.8rem', fontSize: '1.1rem' }}>
                  <strong>HÓA ĐƠN #{inv.bookingId}</strong>
                  &nbsp;|&nbsp;
                  Ngày: {new Date().toLocaleDateString('vi-VN')}
                </p>
              </div>

              {/* Guest info */}
              <h3 style={{ color: '#1f4d45', borderBottom: '1px solid #e0ede9', paddingBottom: '0.4rem' }}>Thông tin khách</h3>
              <table className="invoice-table">
                <tbody>
                  <tr><th>Tên khách</th><td>{inv.guestName}</td></tr>
                  <tr><th>Số điện thoại</th><td>{inv.guestPhone || '—'}</td></tr>
                  <tr><th>Email</th><td>{inv.guestEmail || '—'}</td></tr>
                  <tr><th>Phòng</th><td>{inv.roomNumber}</td></tr>
                  <tr><th>Hạng phòng</th><td>{inv.typeName || '—'}</td></tr>
                  <tr><th>Nhận phòng</th><td>{inv.checkInDate}</td></tr>
                  <tr><th>Trả phòng</th><td>{inv.checkOutDate}</td></tr>
                  {inv.numberOfGuests && <tr><th>Số khách</th><td>{inv.numberOfGuests}</td></tr>}
                </tbody>
              </table>

              {/* Charges */}
              <h3 style={{ color: '#1f4d45', borderBottom: '1px solid #e0ede9', paddingBottom: '0.4rem', marginTop: '1.5rem' }}>Chi tiết thanh toán</h3>
              <table className="invoice-table">
                <thead>
                  <tr>
                    <th>Khoản mục</th>
                    <th style={{ textAlign: 'right' }}>Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td>Tiền phòng cơ bản ({inv.nights ?? '?'} đêm × {money(inv.pricePerNight ?? inv.basePrice)})</td>
                    <td style={{ textAlign: 'right' }}>{money(inv.baseAmount)}</td>
                  </tr>
                  {(inv.extraGuestSurcharge ?? 0) > 0 && (
                    <tr>
                      <td>Phụ thu khách thêm</td>
                      <td style={{ textAlign: 'right' }}>{money(inv.extraGuestSurcharge)}</td>
                    </tr>
                  )}
                  {(inv.weekendSurcharge ?? 0) > 0 && (
                    <tr>
                      <td>Phụ thu cuối tuần / ngày lễ</td>
                      <td style={{ textAlign: 'right' }}>{money(inv.weekendSurcharge)}</td>
                    </tr>
                  )}
                  <tr style={{ background: '#f9f9f9' }}>
                    <td>VAT (10%)</td>
                    <td style={{ textAlign: 'right' }}>{money((inv.totalAmount || 0) * 0.1)}</td>
                  </tr>
                </tbody>
              </table>

              <p className="invoice-total">
                Tổng cộng (đã gồm VAT): {money(inv.totalAmount)}
              </p>

              {/* Payment info */}
              {inv.paymentMethod && (
                <p style={{ color: '#555', marginTop: '0.5rem' }}>
                  Phương thức thanh toán: <strong>{inv.paymentMethod === 'Cash' ? 'Tiền mặt' : inv.paymentMethod === 'Card' ? 'Thẻ ngân hàng' : inv.paymentMethod}</strong>
                </p>
              )}

              <p style={{ marginTop: '2rem', textAlign: 'center', color: '#888', fontSize: '0.85rem' }}>
                Cảm ơn quý khách đã lựa chọn HBS Luxury Hotel!
              </p>

              {/* Print button */}
              <div className="no-print" style={{ marginTop: '1.5rem', display: 'flex', gap: '0.75rem', justifyContent: 'center' }}>
                <button className="btn" onClick={() => window.print()}>🖨 In hóa đơn / Xuất PDF</button>
                <button className="btn secondary" onClick={() => window.history.back()}>← Quay lại</button>
              </div>
            </div>
          )}
        </AsyncView>
      </div>
    </>
  );
}
