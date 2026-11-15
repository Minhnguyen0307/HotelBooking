import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import { money } from '../format';
import { AsyncView, useLoad } from '../ui';

function discountText(promo) {
  return promo.discountPercent > 0
    ? `Giảm ${promo.discountPercent}%`
    : `Giảm ${money(promo.discountAmount)}`;
}

function expiryText(value) {
  if (!value) return 'Không giới hạn ngày hết hạn';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? `Hết hạn ${value}`
    : `Hết hạn ${date.toLocaleDateString('vi-VN')}`;
}

export function CustomerPromosPage() {
  const state = useLoad(() => api('/api/bookings/promos'), []);
  const [copied, setCopied] = useState('');

  async function copyCode(code) {
    try {
      await navigator.clipboard.writeText(code);
      setCopied(code);
      window.setTimeout(() => setCopied((current) => current === code ? '' : current), 1800);
    } catch {
      setCopied('');
    }
  }

  return (
    <section className="page customer-promos-page">
      <div className="customer-promos-heading">
        <div>
          <p className="eyebrow">Dành riêng cho bạn</p>
          <h1>Ưu đãi của tôi</h1>
          <p className="muted">Các mã bạn có thể sử dụng khi đặt phòng được tổng hợp tại đây.</p>
        </div>
        <Link className="btn" to="/search">Tìm phòng</Link>
      </div>
      <AsyncView state={state}>
        {(promos) => promos.length ? (
          <div className="customer-promo-grid">
            {promos.map((promo) => (
              <article className="customer-promo-card" key={promo.code}>
                <div className="customer-promo-topline">
                  <span className="customer-promo-discount">{discountText(promo)}</span>
                  {promo.isNewCustomerOnly && <span className="customer-promo-badge">Khách mới</span>}
                </div>
                <div className="customer-promo-code-row">
                  <strong>{promo.code}</strong>
                  <button className="btn small secondary" type="button" onClick={() => copyCode(promo.code)}>
                    {copied === promo.code ? 'Đã sao chép' : 'Sao chép mã'}
                  </button>
                </div>
                <p className="customer-promo-condition">Đơn tối thiểu {money(promo.minimumOrderAmount || 0)}</p>
                <p className="customer-promo-expiry">{expiryText(promo.expiresAt)}</p>
                <Link className="customer-promo-cta" to="/search">Chọn phòng để sử dụng <span aria-hidden="true">→</span></Link>
              </article>
            ))}
          </div>
        ) : (
          <div className="customer-promos-empty">
            <span aria-hidden="true">✦</span>
            <h2>Chưa có mã ưu đãi phù hợp</h2>
            <p className="muted">Khi có mã dành cho bạn, mã sẽ xuất hiện ở đây để bạn dễ dàng chọn khi đặt phòng.</p>
            <Link className="btn secondary" to="/search">Khám phá phòng</Link>
          </div>
        )}
      </AsyncView>
    </section>
  );
}
