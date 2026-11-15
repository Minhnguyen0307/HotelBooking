import { useState } from 'react';
import { api } from '../api';
import { AsyncView, StarRow, useLoad } from '../ui';

export function AdminReviewsPage() {
  const [tick, setTick] = useState(0);
  const [filter, setFilter] = useState('all'); // 'all', 'unreplied', 'low', 'high'
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [replyingId, setReplyingId] = useState(null);
  const [replyText, setReplyText] = useState('');
  const [busy, setBusy] = useState(false);

  const state = useLoad(() => api('/api/admin/reviews'), [tick]);

  function refresh(msg) {
    setError('');
    setNotice(msg || '');
    setReplyingId(null);
    setReplyText('');
    setTick((t) => t + 1);
  }

  async function handleReply(e, reviewId) {
    e.preventDefault();
    if (!replyText.trim()) return;
    setBusy(true);
    setError('');
    try {
      const res = await api(`/api/admin/reviews/${reviewId}/reply`, {
        method: 'POST',
        body: { response: replyText.trim() },
      });
      refresh(res.message);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete(reviewId, guestName) {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa bài đánh giá của khách '${guestName}'? Thao tác này không thể hoàn tác.`)) return;
    setError('');
    try {
      const res = await api(`/api/admin/reviews/${reviewId}`, { method: 'DELETE' });
      refresh(res.message);
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div className="stack" style={{ maxWidth: 1000, margin: '0 auto' }}>
      <div className="spread">
        <div>
          <h1>Quản lý & Kiểm duyệt Đánh giá</h1>
          <p className="muted">Theo dõi mức độ hài lòng của khách hàng, phản hồi chính thức và kiểm duyệt nội dung.</p>
        </div>
      </div>

      {notice && <div className="banner ok">{notice}</div>}
      {error && <div className="banner">{error}</div>}

      <AsyncView state={state}>
        {(reviews) => {
          const all = reviews || [];
          const totalReviews = all.length;
          const avgRating = totalReviews > 0
            ? (all.reduce((s, r) => s + r.rating, 0) / totalReviews).toFixed(1)
            : '0.0';
          const unrepliedCount = all.filter((r) => !r.staffResponse).length;

          // Apply filter
          let filtered = all;
          if (filter === 'unreplied') filtered = all.filter((r) => !r.staffResponse);
          else if (filter === 'low') filtered = all.filter((r) => r.rating <= 2);
          else if (filter === 'high') filtered = all.filter((r) => r.rating >= 4);

          return (
            <>
              {/* Summary KPIs */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '1rem' }}>
                <div className="panel" style={{ textAlign: 'center', padding: '1rem' }}>
                  <span className="muted">Tổng đánh giá</span>
                  <div style={{ fontSize: '1.8rem', fontWeight: 700, color: 'var(--ink)' }}>{totalReviews}</div>
                </div>
                <div className="panel" style={{ textAlign: 'center', padding: '1rem' }}>
                  <span className="muted">Điểm trung bình</span>
                  <div style={{ fontSize: '1.8rem', fontWeight: 700, color: '#c4a574' }}>
                    ★ {avgRating} <span style={{ fontSize: '0.9rem', color: 'var(--muted)' }}>/ 5</span>
                  </div>
                </div>
                <div className="panel" style={{ textAlign: 'center', padding: '1rem' }}>
                  <span className="muted">Chưa phản hồi</span>
                  <div style={{ fontSize: '1.8rem', fontWeight: 700, color: unrepliedCount > 0 ? '#9d3b32' : '#1f6b45' }}>
                    {unrepliedCount}
                  </div>
                </div>
              </div>

              {/* Filter Tabs */}
              <div className="tabs" style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem' }}>
                <button
                  type="button"
                  className={`tab-btn ${filter === 'all' ? 'active' : ''}`}
                  onClick={() => setFilter('all')}
                >
                  Tất cả ({totalReviews})
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filter === 'unreplied' ? 'active' : ''}`}
                  onClick={() => setFilter('unreplied')}
                >
                  Chưa phản hồi ({unrepliedCount})
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filter === 'low' ? 'active' : ''}`}
                  onClick={() => setFilter('low')}
                >
                  Cần lưu ý (1-2★)
                </button>
                <button
                  type="button"
                  className={`tab-btn ${filter === 'high' ? 'active' : ''}`}
                  onClick={() => setFilter('high')}
                >
                  Đánh giá tốt (4-5★)
                </button>
              </div>

              {/* Reviews List */}
              {filtered.length === 0 ? (
                <div className="panel" style={{ textAlign: 'center', padding: '2rem' }}>
                  <p className="muted">Không có đánh giá nào phù hợp với bộ lọc.</p>
                </div>
              ) : (
                <div className="stack" style={{ gap: '1rem' }}>
                  {filtered.map((item) => (
                    <article className="panel" key={item.reviewId} style={{ position: 'relative' }}>
                      <div className="spread" style={{ alignItems: 'flex-start' }}>
                        <div>
                          <strong>{item.userName}</strong>
                          <span className="muted" style={{ marginLeft: '0.75rem' }}>
                            Phòng {item.roomNumber || '–'}
                          </span>
                          <span className="muted" style={{ marginLeft: '0.75rem', fontSize: '0.85rem' }}>
                            {new Date(item.createdAt).toLocaleDateString('vi-VN')}
                          </span>
                        </div>
                        <div className="row" style={{ gap: '0.5rem', alignItems: 'center' }}>
                          <StarRow value={item.rating} />
                          <button
                            className="btn small"
                            type="button"
                            style={{ background: '#9d3b32', borderColor: '#9d3b32', padding: '0.2rem 0.5rem' }}
                            title="Xóa đánh giá này"
                            onClick={() => handleDelete(item.reviewId, item.userName)}
                          >
                            Xóa
                          </button>
                        </div>
                      </div>

                      <p style={{ margin: '0.75rem 0', lineHeight: 1.5 }}>
                        {item.comment}
                      </p>

                      {/* Attached images */}
                      {item.imageUrls && item.imageUrls.length > 0 && (
                        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', margin: '0.5rem 0' }}>
                          {item.imageUrls.map((url, idx) => (
                            <img
                              key={idx}
                              src={url}
                              alt="Review attachment"
                              style={{ width: 80, height: 80, objectFit: 'cover', borderRadius: 6, border: '1px solid var(--line)' }}
                            />
                          ))}
                        </div>
                      )}

                      {/* Staff Response */}
                      {item.staffResponse && replyingId !== item.reviewId && (
                        <div style={{ background: '#f5f7f6', padding: '0.75rem 1rem', borderRadius: 6, marginTop: '0.75rem', borderLeft: '3px solid #1f4d45' }}>
                          <div className="spread">
                            <strong style={{ fontSize: '0.85rem', color: '#1f4d45' }}>
                              Phản hồi từ {item.staffResponderName || 'Khách sạn'}:
                            </strong>
                            <button
                              className="text-btn"
                              style={{ fontSize: '0.8rem' }}
                              type="button"
                              onClick={() => { setReplyingId(item.reviewId); setReplyText(item.staffResponse); }}
                            >
                              Sửa phản hồi
                            </button>
                          </div>
                          <p style={{ margin: '0.3rem 0 0', fontSize: '0.9rem', color: '#333' }}>
                            {item.staffResponse}
                          </p>
                        </div>
                      )}

                      {/* Reply Form */}
                      {(!item.staffResponse || replyingId === item.reviewId) && (
                        <div style={{ marginTop: '0.75rem' }}>
                          {replyingId === item.reviewId ? (
                            <form onSubmit={(e) => handleReply(e, item.reviewId)}>
                              <div style={{ display: 'flex', gap: '0.5rem' }}>
                                <input
                                  value={replyText}
                                  onChange={(e) => setReplyText(e.target.value)}
                                  placeholder="Nhập nội dung phản hồi chính thức của khách sạn..."
                                  required
                                  style={{ flex: 1 }}
                                />
                                <button className="btn small" type="submit" disabled={busy}>
                                  {busy ? 'Đang lưu...' : 'Lưu phản hồi'}
                                </button>
                                <button
                                  className="btn small secondary"
                                  type="button"
                                  onClick={() => { setReplyingId(null); setReplyText(''); }}
                                >
                                  Đóng
                                </button>
                              </div>
                            </form>
                          ) : (
                            <button
                              className="btn small secondary"
                              type="button"
                              onClick={() => { setReplyingId(item.reviewId); setReplyText(''); }}
                            >
                              Phản hồi đánh giá này
                            </button>
                          )}
                        </div>
                      )}
                    </article>
                  ))}
                </div>
              )}
            </>
          );
        }}
      </AsyncView>
    </div>
  );
}
