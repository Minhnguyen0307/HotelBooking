import { useEffect, useState } from 'react';
import { api } from '../api';
import { AsyncView, useLoad } from '../ui';

export function AdminSettingsPage() {
  const [tick, setTick] = useState(0);
  const [weekendMarkup, setWeekendMarkup] = useState(15);
  const [holidayMarkup, setHolidayMarkup] = useState(25);
  const [holidays, setHolidays] = useState([]);
  const [newHoliday, setNewHoliday] = useState('');
  const [saving, setSaving] = useState(false);
  const [triggering, setTriggering] = useState(false);
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');

  const state = useLoad(async () => {
    const data = await api('/api/admin/settings/pricing-policy');
    setWeekendMarkup(data.weekendMarkupPercent ?? 15);
    setHolidayMarkup(data.holidayMarkupPercent ?? 25);
    setHolidays(data.holidays || ['01-01', '30-04', '01-05', '02-09']);
    return data;
  }, [tick]);

  async function handleSavePolicy(e) {
    e.preventDefault();
    setSaving(true);
    setNotice('');
    setError('');
    try {
      const res = await api('/api/admin/settings/pricing-policy', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          weekendMarkupPercent: Number(weekendMarkup),
          holidayMarkupPercent: Number(holidayMarkup),
          holidays: holidays
        })
      });
      setNotice(res.message || 'Đã lưu cấu hình chính sách định giá thành công!');
    } catch (err) {
      setError(err.message || 'Lỗi khi lưu cấu hình');
    } finally {
      setSaving(false);
    }
  }

  function addHolidayDate(e) {
    e.preventDefault();
    const clean = newHoliday.trim();
    if (!clean) return;
    if (!/^\d{2}-\d{2}$/.test(clean)) {
      setError('Định dạng ngày lễ phải là ngày-tháng (dd-MM), ví dụ: 01-01, 30-04, 02-09');
      return;
    }
    if (!holidays.includes(clean)) {
      setHolidays([...holidays, clean]);
    }
    setNewHoliday('');
  }

  function removeHolidayDate(dateStr) {
    setHolidays(holidays.filter((h) => h !== dateStr));
  }

  async function handleTriggerAutomation() {
    setTriggering(true);
    setNotice('');
    setError('');
    try {
      const res = await api('/api/admin/settings/trigger-automation', { method: 'POST' });
      setNotice(res.message);
    } catch (err) {
      setError(err.message || 'Lỗi kích hoạt tiến trình automation');
    } finally {
      setTriggering(false);
    }
  }

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto', padding: '16px' }}>
      <h1>⚙️ Cài đặt Hệ thống & Chính sách</h1>
      {notice && <div className="banner ok" style={{ marginBottom: '16px' }}>{notice}</div>}
      {error && <div className="banner" style={{ marginBottom: '16px' }}>{error}</div>}

      <AsyncView state={state}>
        {() => (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            {/* Dynamic Pricing Settings */}
            <div className="card" style={{ padding: '20px', borderRadius: '8px', background: '#fff', boxShadow: '0 2px 8px rgba(0,0,0,0.06)' }}>
              <h2 style={{ marginTop: 0, color: '#1a365d', display: 'flex', alignItems: 'center', gap: '8px' }}>
                📈 Định giá động (Dynamic Weekend & Holiday Pricing)
              </h2>
              <p style={{ color: '#4a5568', fontSize: '0.95rem' }}>
                Hệ thống tự động cộng thêm tỷ lệ % vào giá phòng cơ bản khi khách chọn các đêm cuối tuần hoặc các ngày lễ Tết đã đăng ký.
              </p>
              
              <form onSubmit={handleSavePolicy} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '16px' }}>
                  <div>
                    <label style={{ display: 'block', fontWeight: 600, marginBottom: '6px' }}>
                      Tăng giá cuối tuần (Thứ 6 & Thứ 7) %:
                    </label>
                    <input
                      type="number"
                      min="0"
                      max="100"
                      step="1"
                      value={weekendMarkup}
                      onChange={(e) => setWeekendMarkup(e.target.value)}
                      style={{ width: '100%', padding: '8px 12px', borderRadius: '4px', border: '1px solid #cbd5e0' }}
                    />
                    <small style={{ color: '#718096' }}>Ví dụ: 15% (nhập 15)</small>
                  </div>

                  <div>
                    <label style={{ display: 'block', fontWeight: 600, marginBottom: '6px' }}>
                      Tăng giá ngày Lễ / Tết %:
                    </label>
                    <input
                      type="number"
                      min="0"
                      max="100"
                      step="1"
                      value={holidayMarkup}
                      onChange={(e) => setHolidayMarkup(e.target.value)}
                      style={{ width: '100%', padding: '8px 12px', borderRadius: '4px', border: '1px solid #cbd5e0' }}
                    />
                    <small style={{ color: '#718096' }}>Ví dụ: 25% (nhập 25)</small>
                  </div>
                </div>

                <div>
                  <label style={{ display: 'block', fontWeight: 600, marginBottom: '6px' }}>
                    Danh sách các ngày Lễ trong năm (định dạng ngày-tháng: dd-MM):
                  </label>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '10px' }}>
                    {holidays.map((h) => (
                      <span
                        key={h}
                        style={{
                          background: '#ebf8ff',
                          color: '#2b6cb0',
                          border: '1px solid #bee3f8',
                          borderRadius: '16px',
                          padding: '4px 12px',
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '6px',
                          fontSize: '0.9rem'
                        }}
                      >
                        📅 {h}
                        <button
                          type="button"
                          onClick={() => removeHolidayDate(h)}
                          style={{
                            background: 'transparent',
                            border: 'none',
                            color: '#e53e3e',
                            cursor: 'pointer',
                            fontWeight: 'bold',
                            padding: '0 2px'
                          }}
                        >
                          ✕
                        </button>
                      </span>
                    ))}
                  </div>

                  <div style={{ display: 'flex', gap: '8px', maxWidth: '300px' }}>
                    <input
                      type="text"
                      placeholder="dd-MM (VD: 02-09)"
                      value={newHoliday}
                      onChange={(e) => setNewHoliday(e.target.value)}
                      style={{ flex: 1, padding: '6px 10px', borderRadius: '4px', border: '1px solid #cbd5e0' }}
                    />
                    <button
                      type="button"
                      className="btn secondary small"
                      onClick={addHolidayDate}
                    >
                      + Thêm ngày
                    </button>
                  </div>
                </div>

                <div style={{ marginTop: '8px' }}>
                  <button
                    type="submit"
                    className="btn"
                    disabled={saving}
                    style={{ background: '#2b6cb0', color: '#fff', border: 'none', padding: '10px 20px', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 }}
                  >
                    {saving ? 'Đang lưu...' : '💾 Lưu cấu hình định giá'}
                  </button>
                </div>
              </form>
            </div>

            {/* Email Automation Section */}
            <div className="card" style={{ padding: '20px', borderRadius: '8px', background: '#fff', boxShadow: '0 2px 8px rgba(0,0,0,0.06)' }}>
              <h2 style={{ marginTop: 0, color: '#2c5282', display: 'flex', alignItems: 'center', gap: '8px' }}>
                ✉️ Tự động hóa Email (Background Worker Automation)
              </h2>
              <p style={{ color: '#4a5568', fontSize: '0.95rem' }}>
                Hệ thống chạy ngầm định kỳ (Background Service) tự động quét và gửi email chăm sóc khách hàng:
              </p>
              <ul style={{ color: '#4a5568', fontSize: '0.92rem', lineHeight: '1.6', margin: '0 0 16px 20px' }}>
                <li>
                  <strong>Email nhắc nhở nhận phòng (1 ngày trước check-in):</strong> Cung cấp mã đặt phòng, hướng dẫn đường đi, giờ nhận phòng (14:00) và hotline hỗ trợ.
                </li>
                <li>
                  <strong>Email cảm ơn & xin đánh giá (sau khi check-out):</strong> Gửi lời tri ân sau khi trả phòng và đính kèm liên kết viết đánh giá cho khách sạn.
                </li>
              </ul>
              <div>
                <button
                  type="button"
                  className="btn"
                  onClick={handleTriggerAutomation}
                  disabled={triggering}
                  style={{ background: '#319795', color: '#fff', border: 'none', padding: '10px 18px', borderRadius: '6px', cursor: 'pointer', fontWeight: 600 }}
                >
                  {triggering ? 'Đang kích hoạt gửi...' : '⚡ Kích hoạt quét & gửi Email ngay (Thủ công)'}
                </button>
                <small style={{ display: 'block', color: '#718096', marginTop: '6px' }}>
                  * Nhấn nút này để thực thi việc quét và gửi email ngay lập tức thay vì chờ chu kỳ tự động.
                </small>
              </div>
            </div>

            {/* Real-time SignalR Status Section */}
            <div className="card" style={{ padding: '20px', borderRadius: '8px', background: '#fff', boxShadow: '0 2px 8px rgba(0,0,0,0.06)' }}>
              <h2 style={{ marginTop: 0, color: '#276749', display: 'flex', alignItems: 'center', gap: '8px' }}>
                🔔 Chuông thông báo Real-time (SignalR RoomHub)
              </h2>
              <p style={{ color: '#4a5568', fontSize: '0.95rem', margin: '0 0 10px 0' }}>
                Hệ thống SignalR Real-time kết nối WebSocket trực tiếp đến <code>/roomHub</code>. Khi có bất kỳ khách hàng nào đặt phòng mới trên website:
              </p>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', background: '#f0fff4', border: '1px solid #c6f6d5', padding: '12px 16px', borderRadius: '6px', color: '#22543d' }}>
                <span style={{ fontSize: '1.4rem' }}>🔊</span>
                <div>
                  <strong>Chuông thông báo âm thanh (Chime audio) & Popup nổi tự động hiển thị</strong>
                  <div style={{ fontSize: '0.88rem' }}>Màn hình Lễ tân và Quản trị viên sẽ lập tức đổ chuông và xuất hiện thông báo: "Có đơn đặt phòng mới #..." kèm nút xem ngay!</div>
                </div>
              </div>
            </div>
          </div>
        )}
      </AsyncView>
    </div>
  );
}
