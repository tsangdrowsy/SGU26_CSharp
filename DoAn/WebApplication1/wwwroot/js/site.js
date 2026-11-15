document.addEventListener('DOMContentLoaded', function () {
    // ========== SEARCH FUNCTIONALITY (AJAX API) ==========
    const searchToggle = document.getElementById('searchToggle');
    const searchToggleMobile = document.getElementById('searchToggleMobile');
    const closeSearchBtn = document.getElementById('closeSearchBtn');
    const searchOverlay = document.getElementById('searchOverlay');
    const searchInput = document.getElementById('searchInput');
    const suggestionsContainer = document.getElementById('searchSuggestions');
    const suggestionsList = document.getElementById('suggestionsList');
    const defaultHeader = document.getElementById('defaultHeader');
    const searchHeader = document.getElementById('searchHeader');

    // Hàm Debounce (Chờ người dùng ngừng gõ mới gọi API)
    function debounce(func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => func.apply(this, args), delay);
        };
    }

    // Hàm gọi API C# (SearchController)
    async function fetchSearchSuggestions(query) {
        if (!query || query.length < 1) {
            if (suggestionsContainer) suggestionsContainer.classList.remove('active');
            return;
        }

        try {
            // Gọi lên API của ASP.NET
            const response = await fetch(`/api/search?q=${encodeURIComponent(query)}`);
            const result = await response.json();

            if (result.success && result.data.length > 0) {
                const limitedResults = result.data;

                suggestionsList.innerHTML = limitedResults.map(product => `
                    <a href="${product.url}" class="suggestion-item">
                        <img src="/${product.image}" alt="${product.name}" loading="lazy"
                             onerror="this.src='/img/sanpham/placeholder.svg'">
                        <div class="suggestion-info">
                            <h4>${product.name}</h4>
                            <div class="price-wrapper">
                                <span class="price">${product.price}</span>
                            </div>
                            <div class="meta-info">
                                <span class="category">${product.category}</span>
                                ${product.brand ? `<span class="brand">• ${product.brand}</span>` : ''}
                            </div>
                            <div class="stock-status ${product.inStock ? 'in-stock' : 'out-of-stock'}">
                                ${product.inStock
                        ? `<i class="fas fa-check-circle"></i> Còn hàng (${product.stock_quantity})`
                        : '<i class="fas fa-times-circle"></i> Hết hàng'
                    }
                            </div>
                        </div>
                    </a>
                `).join('');

                if (result.count >= 8) {
                    suggestionsList.innerHTML += `
                        <a href="/View/Shop?Search=${encodeURIComponent(query)}" class="view-all-link">
                            <i class="fas fa-search"></i> Xem tất cả kết quả
                        </a>
                    `;
                }
                if (suggestionsContainer) suggestionsContainer.classList.add('active');
            } else {
                suggestionsList.innerHTML = `
                    <div class="no-results">
                        <i class="fas fa-search mb-2 text-gray-400 text-2xl"></i>
                        <p class="text-gray-600 font-medium">Không tìm thấy sản phẩm</p>
                        <p class="text-xs text-gray-400 mt-1">Thử tìm với từ khóa khác</p>
                    </div>
                `;
                if (suggestionsContainer) suggestionsContainer.classList.add('active');
            }
        } catch (error) {
            console.error('Lỗi tìm kiếm:', error);
            suggestionsList.innerHTML = `
                <div class="no-results">
                    <i class="fas fa-exclamation-triangle mb-2 text-orange-400 text-2xl"></i>
                    <p class="text-gray-600">Đã có lỗi xảy ra</p>
                </div>
            `;
            if (suggestionsContainer) suggestionsContainer.classList.add('active');
        }
    }

    const debouncedSearch = debounce(fetchSearchSuggestions, 300);

    // Mở Search
    function enableSearch() {
        document.body.classList.add('search-active');
        if (defaultHeader) defaultHeader.classList.add('hidden');
        if (searchHeader) {
            searchHeader.classList.remove('hidden');
            searchHeader.classList.add('flex');
        }
        if (searchOverlay) {
            searchOverlay.classList.remove('opacity-0', 'pointer-events-none');
            searchOverlay.classList.add('opacity-100', 'pointer-events-auto');
        }
        setTimeout(() => searchInput?.focus(), 100);
    }

    // Đóng Search
    function disableSearch() {
        document.body.classList.remove('search-active');
        if (suggestionsContainer) suggestionsContainer.classList.remove('active');

        if (defaultHeader) defaultHeader.classList.remove('hidden');
        if (searchHeader) {
            searchHeader.classList.add('hidden');
            searchHeader.classList.remove('flex');
        }
        if (searchOverlay) {
            searchOverlay.classList.add('opacity-0', 'pointer-events-none');
            searchOverlay.classList.remove('opacity-100', 'pointer-events-auto');
        }
        if (searchInput) searchInput.value = '';
    }

    // Gắn sự kiện
    if (searchToggle) searchToggle.addEventListener('click', enableSearch);
    if (searchToggleMobile) searchToggleMobile.addEventListener('click', enableSearch);
    if (closeSearchBtn) closeSearchBtn.addEventListener('click', disableSearch);
    if (searchOverlay) searchOverlay.addEventListener('click', disableSearch);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.body.classList.contains('search-active')) {
            disableSearch();
        }
    });

    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            debouncedSearch(e.target.value.trim());
        });

        // Nhấn Enter để chuyển sang trang Shop
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = searchInput.value.trim();
                if (query) {
                    window.location.href = `/View/Shop?Search=${encodeURIComponent(query)}`;
                }
            }
        });
    }

    // Click ra ngoài để đóng suggestions
    document.addEventListener('click', function (e) {
        if (searchInput && suggestionsContainer &&
            !searchInput.contains(e.target) &&
            !suggestionsContainer.contains(e.target)) {
            suggestionsContainer.classList.remove('active');
        }
    });
});

// ========== DESKTOP MEGA MENU ==========
const menuTrigger = document.getElementById('mega-menu-trigger');
const menuDropdown = document.getElementById('mega-menu-dropdown');
const menuItems = document.querySelectorAll('.icon-box-menu[data-menu]');
const menuContents = document.querySelectorAll('.menu-content');

if (menuTrigger) {
    menuTrigger.addEventListener('click', function (e) {
        e.stopPropagation();
        menuDropdown.classList.toggle('hidden');
    });
}

menuItems.forEach(item => {
    item.addEventListener('click', function (e) {
        e.stopPropagation();
        const menuId = this.getAttribute('data-menu');

        // Xóa trạng thái active của tất cả các mục
        menuItems.forEach(el => {
            el.classList.remove('active', 'bg-red-50');
            const titleEl = el.querySelector('.font-bold');
            if (titleEl) titleEl.classList.remove('text-red-600');
        });

        // Thêm trạng thái active cho mục được chọn
        this.classList.add('active', 'bg-red-50');
        const activeTitle = this.querySelector('.font-bold');
        if (activeTitle) activeTitle.classList.add('text-red-600');

        // Ẩn tất cả nội dung bên phải
        menuContents.forEach(content => content.classList.add('hidden'));

        // Hiển thị nội dung tương ứng
        const activeContent = document.getElementById(`content-${menuId}`);
        if (activeContent) activeContent.classList.remove('hidden');
    });
});

// Đóng Mega Menu khi click ra ngoài
document.addEventListener('click', function (e) {
    if (menuDropdown && menuTrigger &&
        !menuDropdown.contains(e.target) && !menuTrigger.contains(e.target)) {
        menuDropdown.classList.add('hidden');
    }
});

// Ngăn việc click bên trong Menu làm đóng Menu
if (menuDropdown) {
    menuDropdown.addEventListener('click', function (e) {
        e.stopPropagation();
    });
}

// ========== MOBILE MENU (off-canvas) ==========
document.addEventListener('DOMContentLoaded', function () {
    const menu = document.getElementById('main-menu');
    if (!menu) return;
    const open = () => { menu.classList.add('open'); document.body.classList.add('menu-open'); };
    const close = () => { menu.classList.remove('open'); document.body.classList.remove('menu-open'); };
    document.querySelectorAll('.menu-toggle').forEach(b => b.addEventListener('click', open));
    menu.querySelectorAll('.close-menu, .close-on-click').forEach(b => b.addEventListener('click', close));
    document.addEventListener('keydown', e => { if (e.key === 'Escape') close(); });
});

// ========== ACCESSIBILITY: ẩn icon trang trí khỏi trình đọc màn hình ==========
// Icon Font Awesome chỉ để minh hoạ; nút chỉ có icon đã có aria-label riêng.
(function () {
    const mark = root => root.querySelectorAll?.('i[class*="fa-"]:not([aria-hidden])')
        .forEach(i => i.setAttribute('aria-hidden', 'true'));
    document.addEventListener('DOMContentLoaded', () => {
        mark(document);
        // Áp dụng cả cho nội dung nạp bằng AJAX (lưới sản phẩm, gợi ý tìm kiếm...)
        new MutationObserver(list => list.forEach(m => m.addedNodes.forEach(n => n.nodeType === 1 && mark(n.parentNode || n))))
            .observe(document.body, { childList: true, subtree: true });
    });
})();
