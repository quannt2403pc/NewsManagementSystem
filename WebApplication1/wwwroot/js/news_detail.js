document.addEventListener('DOMContentLoaded', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const articleId = urlParams.get('id');

    if (!articleId) {
        document.body.innerHTML = '<p class="text-center mt-5">Lỗi: Không tìm thấy ID bài báo.</p>';
        return;
    }

    const apiUrl = `https://localhost:7134/api/NewsArticle/details/${articleId}`;

    try {
        const response = await fetch(apiUrl);
        const loadingSpinner = document.getElementById('loading');
        loadingSpinner.style.display = 'none';

        if (!response.ok) {
            document.body.innerHTML = `<p class="text-center mt-5">Lỗi: Bài báo không tồn tại hoặc không được công khai.</p>`;
            return;
        }

        const article = await response.json();

        // Hiển thị nội dung
        document.getElementById('newsTitle').textContent = article.newsTitle;
        document.getElementById('headline').textContent = article.headline;
        document.getElementById('newsContent').textContent = article.newsContent;
        document.getElementById('newsSource').textContent = article.newsSource;
        document.getElementById('categoryName').textContent = article.category?.categoryName || 'N/A';

        // Hiển thị thông tin ngày và tác giả
        const metaInfo = document.getElementById('meta-info');
        if (article.modifiedDate) {
            metaInfo.innerHTML = `Last modified by <strong>${article.updatedBy?.accountName || 'N/A'}</strong> on ${new Date(article.modifiedDate).toLocaleDateString()}`;
        } else {
            metaInfo.innerHTML = `Created by <strong>${article.createdBy?.accountName || 'N/A'}</strong> on ${new Date(article.createdDate).toLocaleDateString()}`;
        }

        // Hiển thị tags
        const tagsContainer = document.getElementById('tagsContainer');
        if (article.tags && article.tags.length > 0) {
            const tagBadges = article.tags.map(tag => `<span class="badge bg-secondary me-1">${tag.tagName}</span>`).join('');
            tagsContainer.innerHTML = tagBadges;
        } else {
            tagsContainer.textContent = 'N/A';
        }

        document.getElementById('article-content').style.display = 'block';

    } catch (error) {
        console.error('Error fetching article details:', error);
        document.body.innerHTML = `<p class="text-center mt-5">Đã xảy ra lỗi khi tải dữ liệu.</p>`;
    }
});