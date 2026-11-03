/* Who Broke It? — site betiği: sürüm bilgisini okur, indirme bağlantısını doldurur. Üçüncü taraf istek yok. */
(function () {
  'use strict';

  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };

  // mobil menü
  var btn = $('.menu-btn');
  var nav = $('nav.main');
  if (btn && nav) {
    btn.addEventListener('click', function () {
      var open = nav.classList.toggle('open');
      btn.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
    $$('a', nav).forEach(function (a) {
      a.addEventListener('click', function () { nav.classList.remove('open'); btn.setAttribute('aria-expanded', 'false'); });
    });
  }

  // görünür olunca yumuşak giriş
  var items = $$('.reveal');
  if ('IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) { if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); } });
    }, { threshold: 0.12 });
    items.forEach(function (el) { io.observe(el); });
  } else {
    items.forEach(function (el) { el.classList.add('in'); });
  }

  // sürüm bilgisi
  function mb(bytes) { return (bytes / (1024 * 1024)).toFixed(1).replace('.', ',') + ' MB'; }

  function setText(sel, text) { $$(sel).forEach(function (el) { el.textContent = text; }); }

  function fill(m) {
    var url = m.url && m.url.charAt(0) === '/' ? m.url : '/builds/' + m.url;
    setText('[data-ver]', 'v' + m.version);
    setText('[data-size]', m.size ? mb(m.size) : '');
    setText('[data-notes]', m.notes || '');
    if (m.date) setText('[data-date]', new Date(m.date + 'T00:00:00Z').toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' }));
    if (m.sha256) {
      setText('[data-sha]', m.sha256);
      var box = $('#hash-box');
      if (box) box.hidden = false;
    }
    $$('[data-dl]').forEach(function (a) {
      a.href = url;
      a.removeAttribute('aria-disabled');
      a.setAttribute('download', '');
    });
    var main = $('#hero-dl-label');
    if (main) main.textContent = 'Windows için indir · v' + m.version;
    var status = $('#dl-status');
    if (status) status.textContent = '';
  }

  function failed() {
    var status = $('#dl-status');
    if (status) status.textContent = 'Sürüm bilgisi şu an alınamadı. Birkaç dakika sonra yeniden deneyin.';
    var main = $('#hero-dl-label');
    if (main) main.textContent = 'İndirme bağlantısı yükleniyor...';
  }

  if (window.fetch) {
    fetch('/builds/latest.json', { cache: 'no-store' })
      .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
      .then(fill)
      .catch(failed);
  } else {
    failed();
  }

  // özet kopyalama
  var copy = $('#copy-sha');
  if (copy) {
    copy.addEventListener('click', function () {
      var t = $('[data-sha]').textContent;
      if (!t) return;
      var done = function () { copy.textContent = 'Kopyalandı'; setTimeout(function () { copy.textContent = 'Kopyala'; }, 1600); };
      if (navigator.clipboard) navigator.clipboard.writeText(t).then(done, done); else done();
    });
  }
})();
