document.addEventListener('DOMContentLoaded', function () {
    // ========== MOBILE MENU TOGGLE ==========
    const menuToggle = document.querySelector('.menu-toggle');
    const closeMenu = document.querySelector('.close-menu');
    const mobileMenu = document.getElementById('main-menu');

    if (menuToggle) {
        menuToggle.addEventListener('click', function () {
            mobileMenu.classList.remove('-translate-x-full');
            document.body.style.overflow = 'hidden';
        });
    }
    if (closeMenu) {
        closeMenu.addEventListener('click', function () {
            mobileMenu.classList.add('-translate-x-full');
            document.body.style.overflow = '';
        });
    }

    // ========== USER DROPDOWN TOGGLE ==========
    const guestUserToggle = document.getElementById('guestUserToggle');
    const guestUserMenu = document.getElementById('guestUserMenu');


    if (guestUserToggle && guestUserMenu) {
        let guestMenuTimeout;
        guestUserToggle.addEventListener('mouseenter', function () {
            clearTimeout(guestMenuTimeout);
            guestUserMenu.classList.add('active');
        });
        guestUserToggle.addEventListener('mouseleave', function () {
            guestMenuTimeout = setTimeout(() => {
                guestUserMenu.classList.remove('active');
            }, 200);
        });
        guestUserMenu.addEventListener('mouseenter', function () {
            clearTimeout(guestMenuTimeout);
        });
        guestUserMenu.addEventListener('mouseleave', function () {
            guestUserMenu.classList.remove('active');
        });
    }

    // Đóng dropdown khi click ra ngoài
    document.addEventListener('click', function (e) {
        if (guestUserMenu && !guestUserToggle?.contains(e.target) && !guestUserMenu.contains(e.target)) {
            guestUserMenu.classList.remove('active');
        }
    });

    // ========== CATEGORY DROPDOWN TOGGLES (MOBILE) ==========
    const categoryToggles = document.querySelectorAll('.category-toggle');
    categoryToggles.forEach(toggle => {
        toggle.addEventListener('click', function (e) {
            e.stopPropagation();
            const category = this.getAttribute('data-category');
            const submenu = document.getElementById(`submenu-${category}`);
            const icon = this.querySelector('.fa-chevron-down, .fa-chevron-up');

            if (submenu) {
                submenu.classList.toggle('hidden');
                if (icon) {
                    if (submenu.classList.contains('hidden')) {
                        icon.classList.remove('fa-chevron-up');
                        icon.classList.add('fa-chevron-down');
                    } else {
                        icon.classList.remove('fa-chevron-down');
                        icon.classList.add('fa-chevron-up');
                    }
                    icon.classList.toggle('rotate-180');
                }
                this.classList.toggle('bg-red-50');
                this.classList.toggle('text-red-600');
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
            menuItems.forEach(el => {
                el.classList.remove('active', 'bg-red-50');
                const titleEl = el.querySelector('.font-bold');
                if (titleEl) titleEl.classList.remove('text-red-600');
            });
            this.classList.add('active', 'bg-red-50');
            const activeTitle = this.querySelector('.font-bold');
            if (activeTitle) activeTitle.classList.add('text-red-600');
            menuContents.forEach(content => content.classList.add('hidden'));
            const activeContent = document.getElementById(`content-${menuId}`);
            if (activeContent) activeContent.classList.remove('hidden');
        });
    });

    document.addEventListener('click', function (e) {
        if (!menuDropdown?.contains(e.target) && !menuTrigger?.contains(e.target)) {
            menuDropdown?.classList.add('hidden');
        }
    });

    // ========== SEARCH FUNCTIONALITY (ĐÃ KHÔI PHỤC API) ==========
    const searchToggle = document.getElementById('searchToggle');
    const searchToggleMobile = document.getElementById('searchToggleMobile');
    const closeSearchBtn = document.getElementById('closeSearchBtn');
    const searchOverlay = document.getElementById('searchOverlay');
    const searchInput = document.getElementById('searchInput');
    const suggestionsContainer = document.getElementById('searchSuggestions');
    const suggestionsList = document.getElementById('suggestionsList');

    function enableSearch() {
        document.body.classList.add('search-active');
        setTimeout(() => searchInput.focus(), 100);
    }

    function disableSearch() {
        document.body.classList.remove('search-active');
        if (suggestionsContainer) suggestionsContainer.classList.remove('active');
        if (searchInput) searchInput.value = '';
    }

    if (searchToggle) searchToggle.addEventListener('click', enableSearch);
    if (searchToggleMobile) searchToggleMobile.addEventListener('click', enableSearch);
    if (closeSearchBtn) closeSearchBtn.addEventListener('click', disableSearch);
    if (searchOverlay) searchOverlay.addEventListener('click', disableSearch);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && document.body.classList.contains('search-active')) {
            disableSearch();
        }
    });

    // Debounce function để giảm số lần gọi API
    function debounce(func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => func.apply(this, args), delay);
        };
    }

    // Hàm fetch sản phẩm từ API (Sau này sẽ trỏ vào Controller C#)
    async function fetchSearchSuggestions(query) {
        if (!query || query.length < 1) {
            if (suggestionsContainer) suggestionsContainer.classList.remove('active');
            return;
        }

        try {
            // ĐÂY LÀ NƠI GỌI API BACKEND CỦA ASP.NET (Sẽ viết sau)
            const response = await fetch(`/api/search?q=${encodeURIComponent(query)}`);
            const result = await response.json();

            if (result.success && result.data.length > 0) {
                const limitedResults = result.data.slice(0, 8);

                suggestionsList.innerHTML = limitedResults.map(product => `
                    <a href="${product.url}" class="suggestion-item">
                        <img src="/${product.image}" alt="${product.name}" loading="lazy"
                             onerror="this.src='/img/sanpham/placeholder.png'">
                        <div class="suggestion-info">
                            <h4>${product.name}</h4>
                            <div class="price-wrapper">
                                <span class="price">${product.price}</span>
                                ${product.old_price ? `<span class="old-price">${product.old_price}</span>` : ''}
                                ${product.discount > 0 ? `<span class="discount-badge">-${product.discount}%</span>` : ''}
                            </div>
                            <div class="meta-info">
                                <span class="category">${product.category}</span>
                                ${product.brand ? `<span class="brand">• ${product.brand}</span>` : ''}
                            </div>
                            <div class="stock-status ${product.inStock ? 'in-stock' : 'out-of-stock'}">
                                ${product.inStock ?
                        `<i class="fas fa-check-circle"></i> Còn hàng (${product.stock_quantity})` :
                        '<i class="fas fa-times-circle"></i> Hết hàng'
                    }
                            </div>
                        </div>
                    </a>
                `).join('');

                if (result.count > 8) {
                    suggestionsList.innerHTML += `
                        <a href="/Shop?search=${encodeURIComponent(query)}" class="view-all-link">
                            <i class="fas fa-search"></i> Xem tất cả ${result.count} kết quả
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
                    <p class="text-gray-600">Đã có lỗi xảy ra (Chưa có API Backend)</p>
                </div>
            `;
            if (suggestionsContainer) suggestionsContainer.classList.add('active');
        }
    }

    const debouncedSearch = debounce(fetchSearchSuggestions, 300);

    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            debouncedSearch(e.target.value.trim());
        });

        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = searchInput.value.trim();
                if (query) {
                    window.location.href = `/Shop?search=${encodeURIComponent(query)}`;
                }
            }
        });
    }

    document.addEventListener('click', function (e) {
        if (searchInput && suggestionsContainer &&
            !searchInput.contains(e.target) &&
            !suggestionsContainer.contains(e.target)) {
            suggestionsContainer.classList.remove('active');
        }
    });
});