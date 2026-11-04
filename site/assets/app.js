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
    // oyuncular kurulum sihirbazını indirir; zip yalnızca oyun içi otomatik güncelleme içindir
    var file = m.installer || m.url;
    var url = file && file.charAt(0) === '/' ? file : '/builds/' + file;
    var size = m.installer ? m.installerSize : m.size;
    var sha = m.installer ? m.installerSha256 : m.sha256;
    setText('[data-ver]', 'v' + m.version);
    setText('[data-size]', size ? mb(size) : '');
    setText('[data-notes]', m.notes || '');
    if (m.date) setText('[data-date]', new Date(m.date + 'T00:00:00Z').toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' }));
    if (sha) {
      setText('[data-sha]', sha);
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
    // sürüm bilgisi alınamasa da düğme sabit adres (/download) ile en son kurulum dosyasını verir
    if (status) status.textContent = 'Sürüm numarası şu an gösterilemiyor; indirme düğmesi yine de en son kurulum dosyasını verir.';
  }

  if (window.fetch) {
    fetch('/builds/latest.json', { cache: 'no-store' })
      .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
      .then(fill)
      .catch(failed);
  } else {
    failed();
  }

  // çevrimiçi sunucu durumu (herkese açık özet)
  var onl = $('#online-status');
  if (onl && window.fetch) {
    fetch('/api/status', { cache: 'no-store' })
      .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
      .then(function (s) {
        onl.hidden = false;
        if (!s.servers) {
          onl.innerHTML = '<strong>Çevrimiçi sunucular şu an kapalı.</strong> Kısa süre içinde tekrar dene; yine de tek kişilik oynayabilirsin.';
        } else if (s.online > 0) {
          onl.innerHTML = '<strong>Şu an ' + s.online + ' kişi çevrimiçi</strong> · ' + s.rooms + ' dolu oda, ' + s.free + ' boş sunucu.';
        } else {
          onl.innerHTML = '<strong>Çevrimiçi sunucular hazır.</strong> Şu an oyunda kimse yok; ilk giren sen ol.';
        }
      })
      .catch(function () {});
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
