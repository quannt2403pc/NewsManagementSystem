document.addEventListener('DOMContentLoaded', () => {
    const searchForm = document.getElementById('searchForm');
    const resultsContainer = document.getElementById('resultsContainer');
    const loadingSpinner = document.getElementById('loading');
    let categorySelect = document.getElementById('category');
    const tagSelect = document.getElementById('tag');

    const apiUrl = 'https://localhost:7134/api/PublicNews/search';
    const categoryApiUrl = 'https://localhost:7134/api/Category';
    const tagApiUrl = 'https://localhost:7134/api/Tag';

    const createNewsCard = (article) => {
        const createdDate = new Date(article.createdDate).toLocaleDateString('vi-VN');
        return `
            <div class="col">
                <div class="card h-100 shadow-sm" onclick="window.location.href='/ui/newsArticleDetail.html?id=${article.newsArticleId}'">
                    <div class="card-body">
                        <h5 class="card-title">${article.newsTitle}</h5>
                        <p class="card-subtitle mb-2 text-muted">${article.headline}</p>
                        <p class="card-text">${article.newsContent}</p>
                    </div>
                    <div class="card-footer bg-light border-0">
                        <small class="text-muted">Đăng vào ngày: ${createdDate}</small>
                    </div>
                </div>
            </div>
        `;
    };

    const renderResults = (articles) => {
        resultsContainer.innerHTML = '';
        if (articles && articles.length > 0) {
            articles.forEach(article => {
                resultsContainer.innerHTML += createNewsCard(article);
            });
        } else {
            resultsContainer.innerHTML = `<div class="col-12"><p class="alert alert-info text-center">Không tìm thấy bài báo nào phù hợp.</p></div>`;
        }
    };

    // Hàm tải các danh mục vào dropdown
     categorySelect = document.getElementById('category');

    // Sửa lại hàm này để gọi đúng API
    const loadCategories = async (searchTerm = '') => {
        try {
            // Tạo URL với tham số search
            const url = `https://localhost:7134/api/Category?search=${encodeURIComponent(searchTerm)}`;

            const response = await fetch(url);

            if (!response.ok) {
                throw new Error(`Lỗi ${response.status} - ${response.statusText}`);
            }

            const data = await response.json();

            // Cấu trúc dữ liệu có thể là một mảng hoặc một đối tượng có thuộc tính $values
            const categories = Array.isArray(data) ? data : (data.$values || []);

            // Đổ dữ liệu vào dropdown
            categorySelect.innerHTML = '<option value="">Tất cả</option>';
            categories.forEach(cat => {
                const option = document.createElement('option');
                option.value = cat.categoryName; // ✅ Lấy giá trị là CategoryName
                option.textContent = cat.categoryName;
                categorySelect.appendChild(option);
            });

        } catch (error) {
            console.error('Lỗi khi tải danh mục:', error);
        }
    };


    searchForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        loadingSpinner.style.display = 'block';
        resultsContainer.innerHTML = '';

        const formData = new FormData(searchForm);
        const search = formData.get('search');
        const categoryName = formData.get('categoryName');
        const tagName = formData.get('tagName');
        const startDate = formData.get('startDate');
        const endDate = formData.get('endDate');

        const queryParams = new URLSearchParams();
        if (search) queryParams.append('search', search);
        if (categoryName) queryParams.append('categoryName', categoryName);
        if (tagName) queryParams.append('tagName', tagName);
        if (startDate) queryParams.append('startDate', startDate);
        if (endDate) queryParams.append('endDate', endDate);

        try {
            const response = await fetch(`${apiUrl}?${queryParams.toString()}`);

            if (response.status === 404) {
                renderResults([]);
                return;
            }

            if (!response.ok) {
                throw new Error(`Lỗi ${response.status} - ${response.statusText}`);
            }

            const data = await response.json();
            const articles = Array.isArray(data) ? data : (data.$values || []);

            renderResults(articles);
        } catch (error) {
            console.error('Lỗi khi tìm kiếm:', error);
            resultsContainer.innerHTML = `<div class="col-12"><p class="alert alert-danger text-center">Đã xảy ra lỗi khi tìm kiếm.</p></div>`;
        } finally {
            loadingSpinner.style.display = 'none';
        }
    });

    // Tải dữ liệu ban đầu
    loadCategories();
    loadTags();
});