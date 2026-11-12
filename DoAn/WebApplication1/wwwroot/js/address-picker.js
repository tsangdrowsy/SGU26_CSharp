// =====================================================================
// Chọn địa chỉ 3 cấp: Tỉnh/Thành phố -> Quận/Huyện -> Phường/Xã
// Nguồn dữ liệu: https://provinces.open-api.vn (bản v1 - địa giới trước 07/2025,
// còn cấp Quận/Huyện, khớp với cột City/District/Ward trong DB).
//
// Cách dùng trong .cshtml:
//   <select asp-for="City"     data-addr="city"     data-current="@Model.City"></select>
//   <select asp-for="District" data-addr="district" data-current="@Model.District"></select>
//   <select asp-for="Ward"     data-addr="ward"     data-current="@Model.Ward"></select>
//   <script src="~/js/address-picker.js"></script>
//
// Giá trị gửi về server là TÊN (VD "Thành phố Hồ Chí Minh") nên backend/DB giữ nguyên.
// =====================================================================
(function () {
    const API = 'https://provinces.open-api.vn/api/v1';
    const LABEL = { city: 'Chọn Tỉnh/Thành phố', district: 'Chọn Quận/Huyện', ward: 'Chọn Phường/Xã' };

    // Cache trong phiên để không gọi lại API mỗi lần đổi lựa chọn
    async function getJson(url) {
        const key = 'addr:' + url;
        try { const hit = sessionStorage.getItem(key); if (hit) return JSON.parse(hit); } catch { }
        const res = await fetch(url);
        if (!res.ok) throw new Error('HTTP ' + res.status);
        const data = await res.json();
        try { sessionStorage.setItem(key, JSON.stringify(data)); } catch { }
        return data;
    }

    function init() {
        const city = document.querySelector('[data-addr="city"]');
        const dist = document.querySelector('[data-addr="district"]');
        const ward = document.querySelector('[data-addr="ward"]');
        if (!city || !dist || !ward) return;

        // Đổ danh sách vào <select>: value = tên (lưu DB), data-code = mã (gọi API cấp dưới)
        function fill(sel, items, label, placeholderText) {
            sel.replaceChildren(new Option(placeholderText || label, ''));
            items.forEach(x => {
                const o = new Option(x.name, x.name);
                o.dataset.code = x.code;
                sel.add(o);
            });
            sel.disabled = items.length === 0;

            // Chọn lại giá trị cũ (thông tin user có sẵn, hoặc form bị lỗi validate)
            // (chỉ làm khi đã có danh sách - lúc xoá trắng thì giữ lại để dùng sau)
            const cur = sel.dataset.current;
            if (cur && items.length) {
                if (![...sel.options].some(o => o.value === cur)) {
                    // Địa chỉ cũ nhập tay không khớp danh sách -> vẫn giữ để không mất dữ liệu
                    sel.add(new Option(cur + ' (địa chỉ đã lưu)', cur), 1);
                }
                sel.value = cur;
                sel.dataset.current = '';
            }
        }
        const codeOf = sel => sel.selectedOptions[0]?.dataset.code;
        // Cấp trên là địa chỉ cũ không có trong danh sách -> giữ nguyên giá trị đã lưu của cấp dưới
        function keepSaved(sel, label) {
            const v = sel.dataset.current;
            sel.dataset.current = '';
            fill(sel, [], label);
            if (v) { sel.add(new Option(v + ' (địa chỉ đã lưu)', v), 1); sel.value = v; sel.disabled = false; }
        }
        const setLoading = (sel, on) => { sel.classList.toggle('opacity-60', on); sel.setAttribute('aria-busy', on); };

        async function loadDistricts() {
            fill(ward, [], LABEL.ward);
            if (!codeOf(city)) { fill(dist, [], LABEL.district); return; }
            setLoading(dist, true);
            try {
                const p = await getJson(`${API}/p/${codeOf(city)}?depth=2`);
                fill(dist, p.districts || [], LABEL.district);
                if (codeOf(dist)) await loadWards();
                else if (dist.value) keepSaved(ward, LABEL.ward);
            } catch {
                fill(dist, [], LABEL.district, 'Lỗi tải Quận/Huyện - thử lại');
                dist.disabled = false;
            } finally { setLoading(dist, false); }
        }

        async function loadWards() {
            if (!codeOf(dist)) { fill(ward, [], LABEL.ward); return; }
            setLoading(ward, true);
            try {
                const d = await getJson(`${API}/d/${codeOf(dist)}?depth=2`);
                fill(ward, d.wards || [], LABEL.ward);
            } catch {
                fill(ward, [], LABEL.ward, 'Lỗi tải Phường/Xã - thử lại');
                ward.disabled = false;
            } finally { setLoading(ward, false); }
        }

        city.addEventListener('change', loadDistricts);
        dist.addEventListener('change', loadWards);
        // Chọn lại ô báo lỗi -> thử tải lại
        dist.addEventListener('focus', () => { if (dist.options.length <= 1 && codeOf(city)) loadDistricts(); });
        ward.addEventListener('focus', () => { if (ward.options.length <= 1 && codeOf(dist)) loadWards(); });

        (async () => {
            setLoading(city, true);
            try {
                fill(city, await getJson(`${API}/p/`), LABEL.city);
                if (codeOf(city)) await loadDistricts();
                else if (city.value) {
                    // Tỉnh cũ không khớp danh sách -> giữ nguyên quận/phường đã lưu
                    keepSaved(dist, LABEL.district); keepSaved(ward, LABEL.ward);
                } else { fill(dist, [], LABEL.district); fill(ward, [], LABEL.ward); }
            } catch {
                fill(city, [], LABEL.city, 'Không tải được danh sách tỉnh - kiểm tra mạng');
                city.disabled = false;
            } finally { setLoading(city, false); }
        })();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
