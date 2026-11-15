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
      <canvas ref={ref} height={80} />
      <p style={{ textAlign: 'center', fontWeight: 700, fontSize: '1.5rem', color, margin: '0.5rem 0 0' }}>
        {pct.toFixed(1)}%
      </p>
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export function ReportsPage() {
  const [error, setError] = useState('');
  const state = useLoad(() => api('/api/reports'), []);
  const chartState = useLoad(() => api('/api/reports/charts'), []);
  const summaryState = useLoad(() => api('/api/reports/summary'), []);

  async function download(path, file) {
    setError('');
    try {
      await downloadFile(path, file);
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="stack" style={{ maxWidth: 1200, margin: '0 auto' }}>
      <div className="spread">
        <div>
          <h1>Báo cáo & Phân tích Doanh nghiệp</h1>
          <p className="muted">Theo dõi chỉ số tăng trưởng, công suất khai thác phòng và hiệu quả kinh doanh.</p>
        </div>
        <div className="row">
          <button className="btn small" type="button" onClick={() => download('/api/reports/revenue.csv', 'BaoCaoDoanhThu.csv')}>
            Xuất CSV doanh thu
          </button>
          <button className="btn small secondary" type="button" onClick={() => download('/api/reports/occupancy.csv', 'BaoCaoTanSuatPhong.csv')}>
            Xuất CSV công suất
          </button>
        </div>
      </div>

      {error && <div className="banner">{error}</div>}

      {/* KPI Cards */}
      <AsyncView state={summaryState}>
        {(summary) => <ExecutiveKPIs summary={summary} />}
      </AsyncView>

      {/* Main Revenue Timeline */}
      <AsyncView state={state}>
        {(data) => <RevenueChart revenue={data.revenue} />}
      </AsyncView>

      {/* Analytics Charts Grid */}
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

      {/* Detailed Tables */}
      <AsyncView state={state}>
        {(data) => (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', gap: '1rem' }}>
            <div className="panel">
              <h2>Doanh thu theo tháng</h2>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr><th>Năm</th><th>Tháng</th><th>Doanh thu</th><th>Đơn đã thanh toán</th></tr>
                  </thead>
                  <tbody>
                    {(data.revenue || []).length === 0 && <tr><td colSpan="4">Chưa có dữ liệu</td></tr>}
                    {(data.revenue || []).map((item) => (
                      <tr key={`${item.year}-${item.month}`}>
                        <td>{item.year}</td><td>{item.month}</td>
                        <td>{money(item.totalRevenue)}</td><td>{item.totalBookingsPaid}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="panel">
              <h2>Công suất theo phòng</h2>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr><th>Số phòng</th><th>Tổng lượt đặt</th><th>Số đêm được thuê</th></tr>
                  </thead>
                  <tbody>
                    {(data.occupancy || []).length === 0 && <tr><td colSpan="3">Chưa có dữ liệu</td></tr>}
                    {(data.occupancy || []).map((item) => (
                      <tr key={item.roomNumber}>
                        <td><strong>{item.roomNumber}</strong></td>
                        <td>{item.totalBookings}</td>
                        <td>{item.totalNightsBooked ?? 0}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}
      </AsyncView>
    </div>
  );
}
