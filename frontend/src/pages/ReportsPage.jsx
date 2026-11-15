import { useState, useEffect, useRef } from 'react';
import { api, downloadFile } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);

// ─── Chart hooks ─────────────────────────────────────────────────────────────

function useChart(canvasRef, buildConfig, deps) {
  useEffect(() => {
    if (!canvasRef.current) return;
    const ctx = canvasRef.current.getContext('2d');
    const chart = new Chart(ctx, buildConfig());
    return () => chart.destroy();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);
}

// ─── Executive KPI Cards ─────────────────────────────────────────────────────

function ExecutiveKPIs({ summary }) {
  if (!summary) return null;
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
      <div className="panel" style={{ borderLeft: '4px solid #1f4d45' }}>
        <span className="muted" style={{ fontSize: '0.85rem' }}>Doanh thu hôm nay</span>
        <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#1f4d45', marginTop: '0.25rem' }}>
          {money(summary.todayRevenue || 0)}
        </div>
      </div>
      <div className="panel" style={{ borderLeft: '4px solid #c4a574' }}>
        <span className="muted" style={{ fontSize: '0.85rem' }}>Doanh thu tháng này</span>
        <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#c4a574', marginTop: '0.25rem' }}>
          {money(summary.monthRevenue || 0)}
        </div>
      </div>
      <div className="panel" style={{ borderLeft: '4px solid #1a2b4c' }}>
        <span className="muted" style={{ fontSize: '0.85rem' }}>Tổng doanh thu tích lũy</span>
        <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#1a2b4c', marginTop: '0.25rem' }}>
          {money(summary.totalRevenue || 0)}
        </div>
      </div>
      <div className="panel" style={{ borderLeft: '4px solid #1f6b45' }}>
        <span className="muted" style={{ fontSize: '0.85rem' }}>Đang lưu trú / Tổng phòng</span>
        <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#1f6b45', marginTop: '0.25rem' }}>
          {summary.occupiedRooms} / {summary.totalRooms}
          <span style={{ fontSize: '0.85rem', fontWeight: 400, color: 'var(--muted)', marginLeft: '0.5rem' }}>
            ({summary.occupancyPercent}%)
          </span>
        </div>
      </div>
      <div className="panel" style={{ borderLeft: '4px solid #9d3b32' }}>
        <span className="muted" style={{ fontSize: '0.85rem' }}>Yêu cầu hủy chờ duyệt</span>
        <div style={{ fontSize: '1.5rem', fontWeight: 700, color: summary.pendingCancels > 0 ? '#9d3b32' : 'var(--ink)', marginTop: '0.25rem' }}>
          {summary.pendingCancels}
        </div>
      </div>
    </div>
  );
}

// ─── Revenue Line Chart ───────────────────────────────────────────────────────

function RevenueChart({ revenue }) {
  const ref = useRef(null);
  const last12 = [...(revenue || [])].slice(-12);
  const labels = last12.map(d => `Tháng ${d.month}/${d.year}`);
  const values = last12.map(d => +(d.totalRevenue / 1_000_000).toFixed(2));

  useChart(ref, () => ({
    type: 'line',
    data: {
      labels,
      datasets: [{
        label: 'Doanh thu (triệu ₫)',
        data: values,
        borderColor: '#1f4d45',
        backgroundColor: 'rgba(31,77,69,0.1)',
        tension: 0.3,
        fill: true,
        pointRadius: 4,
        pointBackgroundColor: '#1f4d45',
      }],
    },
    options: {
      responsive: true,
      plugins: { legend: { position: 'top' } },
      scales: {
        y: {
          beginAtZero: true,
          title: { display: true, text: 'Triệu VND' },
        },
      },
    },
  }), [revenue]);

  return (
    <div className="panel" style={{ marginBottom: '1rem' }}>
      <h2 style={{ marginTop: 0 }}>Doanh thu theo tháng (12 tháng gần nhất)</h2>
      <canvas ref={ref} height={100} />
    </div>
  );
}

// ─── Property Revenue Bar Chart ───────────────────────────────────────────────

function PropertyRevenueChart({ summary }) {
  const ref = useRef(null);
  const list = summary?.propertyRevenues || [];
  const labels = list.map(p => p.propertyName.replace('HBS ', ''));
  const values = list.map(p => +(p.revenue / 1_000_000).toFixed(2));

  useChart(ref, () => ({
    type: 'bar',
    data: {
      labels,
      datasets: [{
        label: 'Doanh thu (triệu ₫)',
        data: values,
        backgroundColor: '#1f4d45',
        borderRadius: 4,
      }],
    },
    options: {
      responsive: true,
      plugins: {
        legend: { display: false },
        title: { display: true, text: 'Doanh thu theo chuỗi 5 cơ sở Đà Nẵng' }
      },
      scales: {
        y: { beginAtZero: true, title: { display: true, text: 'Triệu VNĐ' } }
      }
    },
  }), [summary]);

  return (
    <div className="panel" style={{ flex: 1, minWidth: 300 }}>
      <h2 style={{ marginTop: 0, fontSize: '1rem' }}>Cơ sở Đà Nẵng</h2>
      <canvas ref={ref} height={120} />
    </div>
  );
}

// ─── Payment Methods Doughnut ─────────────────────────────────────────────────

function PaymentMethodsChart({ summary }) {
  const ref = useRef(null);
  const stats = summary?.paymentMethodStats || [];
  const labels = stats.map(s => s.method === 'Cash' ? 'Tiền mặt' : s.method === 'Now' || s.method === 'Card' ? 'Thẻ / Online' : s.method);
  const values = stats.map(s => s.totalAmount);
  const colors = ['#1f4d45', '#c4a574', '#1a2b4c', '#6d645b'];

  useChart(ref, () => ({
    type: 'doughnut',
    data: {
      labels,
      datasets: [{
        data: values,
        backgroundColor: colors.slice(0, labels.length),
        borderWidth: 2,
        borderColor: '#fff',
      }],
    },
    options: {
      responsive: true,
      plugins: {
        legend: { position: 'bottom' },
        title: { display: true, text: 'Cơ cấu hình thức thanh toán' }
      },
    },
  }), [summary]);

  return (
    <div className="panel" style={{ flex: 1, minWidth: 260 }}>
      <h2 style={{ marginTop: 0, fontSize: '1rem' }}>Phương thức thanh toán</h2>
      <canvas ref={ref} height={120} />
    </div>
  );
}

// ─── Doughnut – Room Type ─────────────────────────────────────────────────────

function RoomTypeDoughnut({ chartData }) {
  const ref = useRef(null);
  const stats = chartData?.roomTypeStats || [];
  const labels = stats.map(s => s.typeName);
  const values = stats.map(s => s.totalBookings);
  const colors = ['#1f4d45', '#c4a574', '#9d3b32', '#1a2b4c', '#6d645b'];

  useChart(ref, () => ({
    type: 'doughnut',
    data: {
      labels,
      datasets: [{
        data: values,
        backgroundColor: colors.slice(0, labels.length),
        borderWidth: 2,
        borderColor: '#fff',
      }],
    },
    options: {
      responsive: true,
      plugins: { legend: { position: 'bottom' }, title: { display: true, text: 'Tỷ lệ đặt theo hạng phòng' } },
    },
  }), [chartData]);

  return (
    <div className="panel" style={{ flex: 1, minWidth: 260 }}>
      <h2 style={{ marginTop: 0, fontSize: '1rem' }}>Hạng phòng</h2>
      <canvas ref={ref} height={120} />
    </div>
  );
}

// ─── Occupancy Bar ────────────────────────────────────────────────────────────

function OccupancyBar({ chartData }) {
  const ref = useRef(null);
  const pct = chartData?.occupancyRatePercent ?? 0;
  const color = pct > 70 ? '#1f6b45' : pct > 40 ? '#b5812f' : '#9d3b32';

  useChart(ref, () => ({
    type: 'bar',
    data: {
      labels: ['Công suất phòng'],
      datasets: [{
        label: `${pct.toFixed(1)}%`,
        data: [pct],
        backgroundColor: color,
        borderRadius: 6,
      }],
    },
    options: {
      indexAxis: 'y',
      responsive: true,
      plugins: { legend: { display: false } },
      scales: {
        x: { min: 0, max: 100, title: { display: true, text: '%' } },
      },
    },
  }), [chartData]);

  return (
    <div className="panel" style={{ flex: 1, minWidth: 260 }}>
      <h2 style={{ marginTop: 0, fontSize: '1rem' }}>Công suất phòng tháng</h2>
      <canvas ref={ref} height={120} />
    </div>
  );
}

// ─── Main Reports Page ────────────────────────────────────────────────────────

const TABS = [
  ['property', '🏢 Doanh thu theo cơ sở'],
  ['charts', '📈 Phân tích & Biểu đồ'],
  ['monthly', '📅 Doanh thu theo tháng'],
  ['occupancy', '🛏️ Công suất phòng'],
  ['all', '📊 Tất cả báo cáo']
];

export function ReportsPage() {
  const [activeTab, setActiveTab] = useState('property');
  const [selectedProperty, setSelectedProperty] = useState('');
  const [selectedYear, setSelectedYear] = useState('');
  const [selectedMonth, setSelectedMonth] = useState('');
  const [error, setError] = useState('');

  const state = useLoad(() => {
    const query = new URLSearchParams();
    if (selectedProperty) query.set('propertyId', selectedProperty);
    if (selectedYear) query.set('year', selectedYear);
    if (selectedMonth) query.set('month', selectedMonth);
    const qs = query.toString();
    return api(qs ? `/api/reports?${qs}` : '/api/reports');
  }, [selectedProperty, selectedYear, selectedMonth]);

  const chartState = useLoad(() => api('/api/reports/charts'), []);
  const summaryState = useLoad(() => api('/api/reports/summary'), []);

  async function download(path, file) {
    setError('');
    try {
      await downloadFile(path, file);
    } catch (err) {
      setError(err.message || 'Không thể tải file báo cáo.');
    }
  }

  function togglePropertyFilter(propertyId) {
    const idStr = String(propertyId);
    setSelectedProperty((prev) => (prev === idStr ? '' : idStr));
  }

  function resetFilters() {
    setSelectedProperty('');
    setSelectedYear('');
    setSelectedMonth('');
  }

  return (
    <AsyncView state={state}>
      {(data) => {
        const propertyList = data.properties || [];
        const propertyRevenues = data.propertyRevenue || [];
        const propertyMonthlies = data.propertyMonthlyRevenue || [];
        const monthlyRevenues = data.revenue || [];
        const occupancies = data.occupancy || [];

        // Thống kê nhanh KPI
        const totalRevenue = propertyRevenues.reduce((acc, item) => acc + (Number(item.totalRevenue) || 0), 0);
        const totalBookingsPaid = propertyRevenues.reduce((acc, item) => acc + (Number(item.totalBookingsPaid) || 0), 0);
        const totalNightsBooked = propertyRevenues.reduce((acc, item) => acc + (Number(item.totalNightsBooked) || 0), 0);
        const topProperty = propertyRevenues.length > 0
          ? propertyRevenues.reduce((max, item) => (item.totalRevenue > max.totalRevenue ? item : max), propertyRevenues[0])
          : null;

        // Danh sách năm cho bộ lọc
        const availableYears = Array.from(
          new Set([
            new Date().getFullYear(),
            ...propertyMonthlies.map((item) => item.year),
            ...monthlyRevenues.map((item) => item.year)
          ].filter(Boolean))
        ).sort((a, b) => b - a);

        const propertyCsvUrl = `/api/reports/property-revenue.csv${
          selectedProperty || selectedYear || selectedMonth
            ? `?${new URLSearchParams({
                ...(selectedProperty && { propertyId: selectedProperty }),
                ...(selectedYear && { year: selectedYear }),
                ...(selectedMonth && { month: selectedMonth })
              }).toString()}`
            : ''
        }`;

        const propertyMonthlyCsvUrl = `/api/reports/property-monthly.csv${
          selectedProperty || selectedYear
            ? `?${new URLSearchParams({
                ...(selectedProperty && { propertyId: selectedProperty }),
                ...(selectedYear && { year: selectedYear })
              }).toString()}`
            : ''
        }`;

        return (
          <div className="stack" style={{ maxWidth: 1200, margin: '0 auto' }}>
            <div className="spread">
              <div>
                <p className="eyebrow">Quản trị & Báo cáo</p>
                <h1>Báo cáo kinh doanh</h1>
                <p className="muted">Theo dõi chỉ số tăng trưởng, doanh thu từng cơ sở và hiệu quả kinh doanh chuỗi HBS.</p>
              </div>
              <div className="row">
                <button
                  className="btn small"
                  type="button"
                  onClick={() => download(propertyCsvUrl, 'BaoCaoDoanhThuTheoCoSo.csv')}
                  title="Xuất CSV doanh thu các cơ sở theo bộ lọc"
                >
                  CSV doanh thu cơ sở
                </button>
                <button
                  className="btn small secondary"
                  type="button"
                  onClick={() => download(propertyMonthlyCsvUrl, 'BaoCaoDoanhThuCoSoTheoThang.csv')}
                  title="Xuất CSV chi tiết từng cơ sở theo tháng"
                >
                  CSV cơ sở theo tháng
                </button>
                <button
                  className="btn small secondary"
                  type="button"
                  onClick={() => download('/api/reports/revenue.csv', 'BaoCaoDoanhThu.csv')}
                >
                  CSV tổng tháng
                </button>
                <button
                  className="btn small secondary"
                  type="button"
                  onClick={() => download('/api/reports/occupancy.csv', 'BaoCaoTanSuatPhong.csv')}
                >
                  CSV công suất
                </button>
              </div>
            </div>

            {error && <div className="banner">{error}</div>}

            {/* Thanh Tab Chuyển Báo Cáo */}
            <div className="report-tabs">
              {TABS.map(([key, label]) => (
                <button
                  key={key}
                  type="button"
                  className={`report-tab${activeTab === key ? ' active' : ''}`}
                  onClick={() => setActiveTab(key)}
                >
                  {label}
                </button>
              ))}
            </div>

            {/* BIỂU ĐỒ & PHÂN TÍCH TRỰC QUAN (Tab: charts hoặc all) */}
            {(activeTab === 'charts' || activeTab === 'all') && (
              <section className="block">
                <div className="section-head">
                  <div>
                    <h2>Biểu đồ phân tích trực quan</h2>
                    <p className="muted">Tổng hợp xu hướng doanh thu, cơ cấu phòng và phương thức thanh toán.</p>
                  </div>
                </div>

                <AsyncView state={summaryState}>
                  {(summary) => <ExecutiveKPIs summary={summary} />}
                </AsyncView>

                <RevenueChart revenue={monthlyRevenues} />

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
                  <AsyncView state={summaryState}>
                    {(summary) => (
                      <>
                        <PropertyRevenueChart summary={summary} />
                        <PaymentMethodsChart summary={summary} />
                      </>
                    )}
                  </AsyncView>

                  <AsyncView state={chartState}>
                    {(cd) => (
                      <>
                        <RoomTypeDoughnut chartData={cd} />
                        <OccupancyBar chartData={cd} />
                      </>
                    )}
                  </AsyncView>
                </div>
              </section>
            )}

            {/* PHẦN 1: BÁO CÁO DOANH THU THEO TỪNG CƠ SỞ */}
            {(activeTab === 'property' || activeTab === 'all') && (
              <section className="block">
                <div className="spread">
                  <div>
                    <h2>Doanh thu theo từng cơ sở</h2>
                    <p className="muted">
                      Theo dõi hiệu quả tài chính và số lượt phòng đã phục vụ của từng chi nhánh trong chuỗi HBS.
                    </p>
                  </div>
                  {(selectedProperty || selectedYear || selectedMonth) && (
                    <button className="text-btn" type="button" onClick={resetFilters}>
                      ✕ Xóa bộ lọc
                    </button>
                  )}
                </div>

                {/* Bộ Lọc (Filter Panel) */}
                <div className="panel filter-panel">
                  <div className="form-grid">
                    <label>
                      Cơ sở khách sạn
                      <select
                        value={selectedProperty}
                        onChange={(e) => setSelectedProperty(e.target.value)}
                      >
                        <option value="">-- Tất cả cơ sở --</option>
                        {propertyList.map((prop) => (
                          <option key={prop.propertyId} value={prop.propertyId}>
                            {prop.name} ({prop.area})
                          </option>
                        ))}
                      </select>
                    </label>

                    <label>
                      Năm báo cáo
                      <select
                        value={selectedYear}
                        onChange={(e) => setSelectedYear(e.target.value)}
                      >
                        <option value="">-- Tất cả các năm --</option>
                        {availableYears.map((yr) => (
                          <option key={yr} value={yr}>Năm {yr}</option>
                        ))}
                      </select>
                    </label>

                    <label>
                      Tháng
                      <select
                        value={selectedMonth}
                        onChange={(e) => setSelectedMonth(e.target.value)}
                      >
                        <option value="">-- Cả năm --</option>
                        {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                          <option key={m} value={m}>Tháng {m}</option>
                        ))}
                      </select>
                    </label>

                    <button
                      className="btn secondary"
                      type="button"
                      onClick={() => download(propertyCsvUrl, 'BaoCaoDoanhThuTheoCoSo.csv')}
                    >
                      Xuất bảng này
                    </button>
                  </div>
                </div>

                {/* 4 Thẻ KPI Tổng Quan */}
                <div className="kpi-grid">
                  <div className="kpi-card">
                    <span>Tổng Doanh Thu</span>
                    <strong>{money(totalRevenue)}</strong>
                    <small>
                      {selectedProperty
                        ? `Cơ sở đang lọc`
                        : `${propertyRevenues.length} cơ sở toàn hệ thống`}
                      {selectedYear ? ` · Năm ${selectedYear}` : ''}
                      {selectedMonth ? ` · T${selectedMonth}` : ''}
                    </small>
                  </div>

                  <div className="kpi-card">
                    <span>Cơ Sở Dẫn Đầu</span>
                    <strong style={{ fontSize: '1.4rem' }}>
                      {topProperty && topProperty.totalRevenue > 0
                        ? topProperty.propertyName
                        : 'Chưa có'}
                    </strong>
                    <small>
                      {topProperty && topProperty.totalRevenue > 0
                        ? `${money(topProperty.totalRevenue)} (${topProperty.totalBookingsPaid} đơn)`
                        : 'Chưa có giao dịch'}
                    </small>
                  </div>

                  <div className="kpi-card">
                    <span>Đơn Đã Thanh Toán</span>
                    <strong>{totalBookingsPaid}</strong>
                    <small>Lượt đặt phòng thanh toán thành công</small>
                  </div>

                  <div className="kpi-card">
                    <span>Tổng Số Đêm Thuê</span>
                    <strong>{totalNightsBooked}</strong>
                    <small>Đêm lưu trú thực tế của khách hàng</small>
                  </div>
                </div>

                {/* Danh thiếp trực quan từng cơ sở (Property Board) */}
                <div className="property-board">
                  {propertyRevenues.map((prop, index) => {
                    const isSelected = selectedProperty === String(prop.propertyId);
                    return (
                      <button
                        key={prop.propertyId}
                        className={`property-card${isSelected ? ' selected' : ''}`}
                        type="button"
                        onClick={() => togglePropertyFilter(prop.propertyId)}
                        title="Nhấp để lọc nhanh cơ sở này"
                      >
                        <span className={`property-mark tone-${index % 5}`}>
                          {prop.area ? prop.area.slice(0, 1).toUpperCase() : 'H'}
                        </span>
                        <strong>{prop.propertyName}</strong>
                        <p className="price" style={{ margin: '0.2rem 0' }}>{money(prop.totalRevenue)}</p>
                        <p className="muted" style={{ fontSize: '0.85rem' }}>{prop.area} · {prop.totalRooms} phòng</p>
                        <span className="pill">
                          {prop.totalBookingsPaid} đơn · {prop.totalNightsBooked} đêm
                        </span>
                      </button>
                    );
                  })}
                </div>

                {/* Bảng Doanh Thu Tổng Hợp Từng Cơ Sở */}
                <div className="section-head">
                  <h3>Bảng tổng hợp doanh thu theo cơ sở</h3>
                  <span className="muted" style={{ fontSize: '0.88rem' }}>Đơn vị: VNĐ</span>
                </div>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Cơ sở</th>
                        <th>Khu vực</th>
                        <th>Địa chỉ</th>
                        <th style={{ textAlign: 'center' }}>Số phòng</th>
                        <th style={{ textAlign: 'right' }}>Doanh thu</th>
                        <th style={{ textAlign: 'center' }}>Đơn thanh toán</th>
                        <th style={{ textAlign: 'center' }}>Số đêm thuê</th>
                        <th style={{ textAlign: 'right' }}>TB / đơn</th>
                      </tr>
                    </thead>
                    <tbody>
                      {propertyRevenues.length === 0 && (
                        <tr>
                          <td colSpan="8" style={{ textAlign: 'center' }}>Không có dữ liệu cơ sở phù hợp</td>
                        </tr>
                      )}
                      {propertyRevenues.map((item) => (
                        <tr
                          key={item.propertyId}
                          style={selectedProperty === String(item.propertyId) ? { background: '#f5faf7' } : undefined}
                        >
                          <td>
                            <strong>{item.propertyName}</strong>
                          </td>
                          <td>{item.area}</td>
                          <td className="muted" style={{ fontSize: '0.88rem' }}>{item.address}</td>
                          <td style={{ textAlign: 'center' }}>{item.totalRooms}</td>
                          <td style={{ textAlign: 'right', fontWeight: 600 }} className="price">
                            {money(item.totalRevenue)}
                          </td>
                          <td style={{ textAlign: 'center' }}>{item.totalBookingsPaid}</td>
                          <td style={{ textAlign: 'center' }}>{item.totalNightsBooked}</td>
                          <td style={{ textAlign: 'right' }}>
                            {money(item.averageRevenuePerBooking)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    {propertyRevenues.length > 0 && (
                      <tfoot>
                        <tr>
                          <td colSpan="3">
                            <strong>TỔNG CỘNG ({propertyRevenues.length} cơ sở)</strong>
                          </td>
                          <td style={{ textAlign: 'center' }}>
                            {propertyRevenues.reduce((sum, item) => sum + (item.totalRooms || 0), 0)}
                          </td>
                          <td style={{ textAlign: 'right' }} className="price">
                            <strong>{money(totalRevenue)}</strong>
                          </td>
                          <td style={{ textAlign: 'center' }}>
                            <strong>{totalBookingsPaid}</strong>
                          </td>
                          <td style={{ textAlign: 'center' }}>
                            <strong>{totalNightsBooked}</strong>
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            <strong>
                              {money(totalBookingsPaid > 0 ? Math.round(totalRevenue / totalBookingsPaid) : 0)}
                            </strong>
                          </td>
                        </tr>
                      </tfoot>
                    )}
                  </table>
                </div>

                {/* Bảng Chi Tiết Biến Động Cơ Sở Theo Từng Tháng */}
                <div className="section-head" style={{ marginTop: '2rem' }}>
                  <div>
                    <h3>Chi tiết doanh thu từng cơ sở theo tháng</h3>
                    <p className="muted" style={{ margin: '0.2rem 0 0', fontSize: '0.9rem' }}>
                      Phân rã lịch sử thu ngân của từng chi nhánh theo từng tháng để so sánh tốc độ tăng trưởng.
                    </p>
                  </div>
                  <button
                    className="btn small secondary"
                    type="button"
                    onClick={() => download(propertyMonthlyCsvUrl, 'BaoCaoDoanhThuCoSoTheoThang.csv')}
                  >
                    CSV tháng cơ sở
                  </button>
                </div>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th style={{ textAlign: 'center' }}>Năm</th>
                        <th style={{ textAlign: 'center' }}>Tháng</th>
                        <th>Cơ sở</th>
                        <th>Khu vực</th>
                        <th style={{ textAlign: 'right' }}>Doanh thu</th>
                        <th style={{ textAlign: 'center' }}>Đơn thanh toán</th>
                        <th style={{ textAlign: 'center' }}>Số đêm thuê</th>
                      </tr>
                    </thead>
                    <tbody>
                      {propertyMonthlies.length === 0 && (
                        <tr>
                          <td colSpan="7" style={{ textAlign: 'center' }}>Chưa có phát sinh giao dịch trong kỳ này</td>
                        </tr>
                      )}
                      {propertyMonthlies.map((item, idx) => (
                        <tr key={`${item.propertyId}-${item.year}-${item.month}-${idx}`}>
                          <td style={{ textAlign: 'center' }}>{item.year}</td>
                          <td style={{ textAlign: 'center' }}>Tháng {item.month}</td>
                          <td><strong>{item.propertyName}</strong></td>
                          <td>{item.area}</td>
                          <td style={{ textAlign: 'right', fontWeight: 600 }} className="price">
                            {money(item.totalRevenue)}
                          </td>
                          <td style={{ textAlign: 'center' }}>{item.totalBookingsPaid}</td>
                          <td style={{ textAlign: 'center' }}>{item.totalNightsBooked}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}

            {/* PHẦN 2: DOANH THU THEO THÁNG TOÀN HỆ THỐNG */}
            {(activeTab === 'monthly' || activeTab === 'all') && (
              <section className="block">
                <div className="section-head">
                  <div>
                    <h2>Doanh thu toàn hệ thống theo tháng</h2>
                    <p className="muted">Tổng hợp doanh số từ tất cả các chi nhánh theo trục thời gian.</p>
                  </div>
                  <button
                    className="btn small"
                    type="button"
                    onClick={() => download('/api/reports/revenue.csv', 'BaoCaoDoanhThu.csv')}
                  >
                    CSV doanh thu
                  </button>
                </div>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Năm</th>
                        <th>Tháng</th>
                        <th style={{ textAlign: 'right' }}>Tổng Doanh thu</th>
                        <th style={{ textAlign: 'center' }}>Đơn đã thanh toán</th>
                      </tr>
                    </thead>
                    <tbody>
                      {monthlyRevenues.length === 0 && (
                        <tr><td colSpan="4" style={{ textAlign: 'center' }}>Chưa có dữ liệu</td></tr>
                      )}
                      {monthlyRevenues.map((item) => (
                        <tr key={`${item.year}-${item.month}`}>
                          <td>{item.year}</td>
                          <td>Tháng {item.month}</td>
                          <td style={{ textAlign: 'right', fontWeight: 600 }} className="price">
                            {money(item.totalRevenue)}
                          </td>
                          <td style={{ textAlign: 'center' }}>{item.totalBookingsPaid}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}

            {/* PHẦN 3: CÔNG SUẤT BUỒNG PHÒNG */}
            {(activeTab === 'occupancy' || activeTab === 'all') && (
              <section className="block">
                <div className="section-head">
                  <div>
                    <h2>Tần suất & Công suất buồng phòng</h2>
                    <p className="muted">Số lượt đặt và tổng số đêm được khách thuê của từng phòng.</p>
                  </div>
                  <button
                    className="btn small secondary"
                    type="button"
                    onClick={() => download('/api/reports/occupancy.csv', 'BaoCaoTanSuatPhong.csv')}
                  >
                    CSV công suất
                  </button>
                </div>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Số phòng</th>
                        <th style={{ textAlign: 'center' }}>Tổng lượt đặt</th>
                        <th style={{ textAlign: 'center' }}>Tổng số đêm đã thuê</th>
                      </tr>
                    </thead>
                    <tbody>
                      {occupancies.length === 0 && (
                        <tr><td colSpan="3" style={{ textAlign: 'center' }}>Chưa có dữ liệu</td></tr>
                      )}
                      {occupancies.map((item) => (
                        <tr key={item.roomNumber}>
                          <td><strong>Phòng {item.roomNumber}</strong></td>
                          <td style={{ textAlign: 'center' }}>{item.totalBookings}</td>
                          <td style={{ textAlign: 'center' }}>{item.totalNightsBooked ?? 0}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}
          </div>
        );
      }}
    </AsyncView>
  );
}
