const API_BASE = "https://localhost:7134/api/StaffArticle";
const CATEGORY_API = "https://localhost:7134/api/Category";
const TAG_API = "https://localhost:7134/api/Tag";

document.addEventListener("DOMContentLoaded", () => {
    const token = localStorage.getItem("jwtToken");

    if (!token) {
        alert("Vui lòng đăng nhập lại!");
        window.location.href = "login.html";
        return;
    }

    const headers = {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
    };

    const tableBody = document.getElementById("articleTableBody");
    const searchInput = document.getElementById("searchInput");
    const searchBtn = document.getElementById("searchBtn");
    const addArticleBtn = document.getElementById("addArticleBtn");
    const articleForm = document.getElementById("articleForm");
    const modal = new bootstrap.Modal(document.getElementById("articleModal"));
    const modalTitle = document.getElementById("modalTitle");
    const logoutBtn = document.getElementById("logoutBtn");
    const categorySelect = document.getElementById("articleCategory");
    const tagCheckboxesContainer = document.getElementById("tagCheckboxesContainer");

    let allTags = [];

    // --- Load danh sách thẻ ---
    async function loadTags() {
        try {
            const response = await fetch(TAG_API, { headers });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            allTags = await response.json();

            tagCheckboxesContainer.innerHTML = "";
            allTags.forEach(tag => {
                const checkboxHtml = `
                    <div class="form-check me-3">
                        <input class="form-check-input" type="checkbox" value="${tag.tagId}" id="tag-${tag.tagId}">
                        <label class="form-check-label" for="tag-${tag.tagId}">${tag.tagName}</label>
                    </div>
                `;
                tagCheckboxesContainer.innerHTML += checkboxHtml;
            });
        } catch (error) {
            console.error("Lỗi khi tải tags:", error);
        }
    }

    // --- Load danh mục ---
    async function loadCategories() {
        try {
            const response = await fetch(`${CATEGORY_API}?search=`, { headers });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const categories = await response.json();

            categorySelect.innerHTML = "<option value=''>Chọn một danh mục</option>";
            categories.forEach(cat => {
                if (cat.isActive) {
                    const option = document.createElement("option");
                    option.value = cat.categoryId;
                    option.textContent = cat.categoryName;
                    categorySelect.appendChild(option);
                }
            });
        } catch (err) {
            console.error("Lỗi tải danh mục:", err);
            alert("Không thể tải danh mục. Vui lòng thử lại.");
        }
    }

    // --- Load danh sách bài viết ---
    async function loadArticles(search = "") {
        try {
            const response = await fetch(`${API_BASE}?search=${encodeURIComponent(search)}`, { headers });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const articles = await response.json();
            renderTable(articles);
        } catch (err) {
            console.error("Lỗi tải dữ liệu:", err);
            alert("Không thể tải danh sách bài viết.");
        }
    }

    function renderTable(articles) {
        tableBody.innerHTML = "";
        articles.forEach(article => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td>${article.newsArticleId}</td>
                <td>${article.newsTitle}</td>
                <td>${new Date(article.createdDate).toLocaleDateString()}</td>
                <td>${article.createdBy?.accountName || "—"}</td>
                <td>${article.category?.categoryName || "—"}</td>
                <td class="${article.newsStatus ? "status-active" : "status-inactive"}">
                    ${article.newsStatus ? "Active" : "Inactive"}
                </td>
                <td>
                    <button class="btn btn-sm btn-info me-1" onclick="editArticle(${article.newsArticleId})">Sửa</button>
                    <button class="btn btn-sm btn-danger" onclick="deleteArticle(${article.newsArticleId})">Xóa</button>
                </td>
            `;
            tableBody.appendChild(tr);
        });
    }

    // --- Sự kiện tìm kiếm ---
    searchBtn.addEventListener("click", () => {
        const searchValue = searchInput.value.trim();
        loadArticles(searchValue);
    });

    // --- Sự kiện thêm bài ---
    addArticleBtn.addEventListener("click", async () => {
        modalTitle.textContent = "Thêm Bài báo";
        articleForm.reset();
        document.getElementById("articleId").value = "";

        await loadCategories();
        await loadTags();

        modal.show();
    });

    // --- Submit form (Thêm/Sửa) ---
    articleForm.addEventListener("submit", async (e) => {
        e.preventDefault();

        const id = document.getElementById("articleId").value;
        const selectedTags = Array.from(tagCheckboxesContainer.querySelectorAll('input[type="checkbox"]:checked'))
            .map(cb => parseInt(cb.value));

        // ✅ Dữ liệu gửi khớp backend
        const article = {
            newsTitle: document.getElementById("articleTitle").value,
            headline: document.getElementById("articleHeadline").value,
            newsContent: document.getElementById("articleContent").value,
            newsSource: document.getElementById("articleSource").value,
            categoryId: parseInt(document.getElementById("articleCategory").value),
            newsStatus: document.getElementById("articleStatus").value === "true",
            tagIds: selectedTags

        };
        console.log(article);
        console.log("Token gửi đi:", token);

        try {
            let response;
            if (id) {
                // PUT -> NewsArticleUpdateRequest
                article.newsArticleId = parseInt(id);
                response = await fetch(`${API_BASE}/${id}`, {
                    method: "PUT",
                    headers,
                    body: JSON.stringify(article)
                });
            } else {
                // POST -> NewsArticle
                response = await fetch(API_BASE, {
                    method: "POST",
                    headers,
                    body: JSON.stringify(article)
                });
            }

            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            modal.hide();
            await loadArticles();
        } catch (err) {
            console.error("Lỗi lưu bài:", err);
            alert("Không thể lưu bài báo (HTTP 400 - Kiểm tra dữ liệu nhập)");
        }
    });

    // --- Đăng xuất ---
    logoutBtn.addEventListener("click", () => {
        localStorage.removeItem("jwtToken");
        window.location.href = "/login.html";
    });

    // --- Sửa bài ---
    window.editArticle = async function (id) {
        try {
            const response = await fetch(`${API_BASE}/${id}`, { headers });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const article = await response.json();

            await loadCategories();
            await loadTags();

            document.getElementById("articleId").value = article.newsArticleId;
            document.getElementById("articleTitle").value = article.newsTitle;
            document.getElementById("articleHeadline").value = article.headline || "";
            document.getElementById("articleContent").value = article.newsContent;
            document.getElementById("articleSource").value = article.newsSource || "";
            document.getElementById("articleCategory").value = article.categoryId;
            document.getElementById("articleStatus").value = article.newsStatus.toString();

            // Đánh dấu checkbox thẻ đã gán
            const currentTagIds = article.tags.map(t => t.tagId);
            tagCheckboxesContainer.querySelectorAll('input[type="checkbox"]').forEach(cb => {
                cb.checked = currentTagIds.includes(parseInt(cb.value));
            });

            modalTitle.textContent = "Chỉnh sửa Bài báo";
            modal.show();
        } catch (err) {
            console.error("Lỗi sửa bài báo:", err);
            alert("Không thể tải thông tin bài báo.");
        }
    };
    document.getElementById('logoutBtn').addEventListener('click', () => {
        localStorage.removeItem("jwtToken");
        window.location.href = "/ui/login.html";
    });
    // --- Xóa bài ---
    window.deleteArticle = async function (id) {
        if (!confirm("Bạn có chắc muốn xóa bài báo này không?")) return;
        try {
            const response = await fetch(`${API_BASE}/${id}`, {
                method: "DELETE",
                headers
            });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            alert("Đã xóa thành công");
            await loadArticles();
        } catch (err) {
            console.error("Lỗi xóa:", err);
            alert("Không thể xóa bài báo");
        }
    };

    // --- Khởi tạo ---
    loadArticles();
});
