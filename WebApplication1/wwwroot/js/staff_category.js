const API_BASE = "https://localhost:7134/api/Category";
const token = localStorage.getItem("jwtToken");

// Nếu không có token → quay lại đăng nhập
if (!token) {
    alert("Vui lòng đăng nhập lại!");
    window.location.href = "/ui/login.html";
}

const headers = {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
};

const tableBody = document.getElementById("categoryTableBody");
const searchInput = document.getElementById("searchInput");
const searchBtn = document.getElementById("searchBtn");
const addCategoryBtn = document.getElementById("addCategoryBtn");

const categoryModal = new bootstrap.Modal(document.getElementById("categoryModal"));
const categoryForm = document.getElementById("categoryForm");
const modalTitle = document.getElementById("modalTitle");
const parentCategorySelect = document.getElementById("parentCategory");

// ==========================
// 1️⃣ Load toàn bộ danh mục
// ==========================
let allCategories = [];
async function loadCategories(search = "") {
    try {
        const res = await fetch(`${API_BASE}/with-count${search ? `?search=${encodeURIComponent(search)}` : ""}`, {
            headers
        });
        if (!res.ok) throw new Error(`Lỗi tải dữ liệu: ${res.status}`);
        const data = await res.json();
        console.log(data)
        allCategories = data; // Lưu lại toàn bộ để dùng sau

        renderCategoryTable(data);
        populateParentCategoryOptions(data);
    } catch (err) {
        console.error("❌ Lỗi khi load danh mục:", err);
        alert("Không thể tải danh mục!");
    }
}

// ==========================
// 2️⃣ Hiển thị dữ liệu ra bảng
// ==========================
function renderCategoryTable(categories) {
    tableBody.innerHTML = "";

    if (!categories || categories.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-muted">Không có danh mục nào.</td></tr>`;
        return;
    }

    categories.forEach(cat => {
        const tr = document.createElement("tr");

        // Lấy tên danh mục cha (nếu có), nếu không có thì trả về null
        const parentCategoryName = cat.parentCategoryName ?? null;
        // Lấy số lượng bài viết, nếu không có thì trả về 0
        const articleCount = cat.articleCount ?? 0;
        tr.innerHTML = `
    <td>${cat.categoryID}</td>
    <td>${cat.categoryName}</td>
    <td>${cat.categoryDescription}</td>
    <td>${parentCategoryName ?? "—"}</td>
    <td>
        <span class="${cat.isActive ? "active-status" : "inactive-status"}">
            ${cat.isActive ? "Hoạt động" : "Ẩn"}
        </span>
    </td>
    <td>${articleCount}</td>
    <td>
        <button class="btn btn-sm btn-warning me-1" onclick="openEditModal(${cat.categoryID})">Sửa</button>
        <button class="btn btn-sm btn-danger me-1" onclick="deleteCategory(${cat.categoryID})">Xóa</button>
        <button class="btn btn-sm btn-secondary" onclick="toggleActive(${cat.categoryID})">Bật/Tắt</button>
    </td>
`;
        tableBody.appendChild(tr);
    });
}

// ==========================
// 3️⃣ Tải danh mục cha (cho form thêm/sửa)
// ==========================
function populateParentCategoryOptions(categories, excludeIds = []) {
    parentCategorySelect.innerHTML = `<option value="">Không có danh mục cha</option>`;

    categories
        .filter(cat => !excludeIds.includes(cat.categoryID))
        .forEach(cat => {
            const option = document.createElement("option");
            option.value = cat.categoryID;
            option.textContent = cat.categoryName;
            parentCategorySelect.appendChild(option);
        });
}
document.getElementById('logoutBtn').addEventListener('click', () => {
    localStorage.removeItem("jwtToken");
    window.location.href = "/ui/login.html";
});
// ==========================
// 4️⃣ Thêm / Sửa danh mục
// ==========================
addCategoryBtn.addEventListener("click", () => {
    modalTitle.textContent = "Thêm Danh mục";
    categoryForm.reset();
    document.getElementById("categoryId").value = "";
    categoryModal.show();
});

categoryForm.addEventListener("submit", async (e) => {
    e.preventDefault();

    const categoryId = document.getElementById("categoryId").value;
    const category = {
        categoryName: document.getElementById("categoryName").value.trim(),
        categoryDescription: document.getElementById("categoryDescription").value.trim(),
        parentCategoryId: document.getElementById("parentCategory").value || null
    };

    try {
        let res;
        if (categoryId) {
            // PUT update
            category.categoryID = parseInt(categoryId);
            res = await fetch(`${API_BASE}/${categoryId}`, {
                method: "PUT",
                headers,
                body: JSON.stringify(category)
            });
        } else {
            // POST create
            res = await fetch(API_BASE, {
                method: "POST",
                headers,
                body: JSON.stringify(category)
            });
        }

        if (res.status === 400) {
            const errData = await res.json();
            alert(errData.message || "Lỗi dữ liệu không hợp lệ!");
            return;
        }

        if (!res.ok) throw new Error("Không thể lưu danh mục!");

        categoryModal.hide();
        await loadCategories();
    } catch (err) {
        console.error("❌ Lỗi khi lưu:", err);
        alert("Đã xảy ra lỗi khi lưu danh mục!");
    }
});

// ==========================
// 5️⃣ Mở modal chỉnh sửa
// ==========================
async function openEditModal(id) {
    try {
        const res = await fetch(`${API_BASE}/${id}`, { headers });
        if (!res.ok) throw new Error("Không tìm thấy danh mục!");

        const cat = await res.json();

        // 🧠 Tìm tất cả category con (và cháu) của category đang sửa
        const getDescendants = (parentId, categories) => {
            const children = categories.filter(c => c.parentCategoryId === parentId);
            let all = [...children];
            children.forEach(ch => {
                all = all.concat(getDescendants(ch.categoryID, categories));
            });
            return all;
        };

        const descendants = getDescendants(cat.categoryId, allCategories);
        const excludeIds = [cat.categoryId, ...descendants.map(c => c.categoryID)];

        modalTitle.textContent = "Chỉnh sửa Danh mục";
        document.getElementById("categoryId").value = cat.categoryId;
        document.getElementById("categoryName").value = cat.categoryName;
        document.getElementById("categoryDescription").value = cat.categoryDescription;

        // 🔥 Gọi lại populate nhưng loại bỏ các category không hợp lệ
        populateParentCategoryOptions(allCategories, excludeIds);

        // Chọn lại parent hiện tại (nếu hợp lệ)
        if (cat.parentCategoryId && !excludeIds.includes(cat.parentCategoryId)) {
            document.getElementById("parentCategory").value = cat.parentCategoryId;
        } else {
            document.getElementById("parentCategory").value = "";
        }

        categoryModal.show();
    } catch (err) {
        console.error("❌ Lỗi mở modal:", err);
        alert("Không thể tải thông tin danh mục!");
    }
}

// ==========================
// 6️⃣ Xóa danh mục
// ==========================
async function deleteCategory(id) {
    if (!confirm("Bạn có chắc muốn xóa danh mục này không?")) return;

    try {
        const res = await fetch(`${API_BASE}/${id}`, {
            method: "DELETE",
            headers
        });

        if (res.status === 400) {
            const errData = await res.json();
            alert(errData.message || "Không thể xóa danh mục!");
            return;
        }

        if (!res.ok) throw new Error("Xóa thất bại!");
        await loadCategories();
    } catch (err) {
        console.error("❌ Lỗi xóa danh mục:", err);
        alert("Không thể xóa danh mục!");
    }
}

// ==========================
// 7️⃣ Bật/Tắt danh mục
// ==========================
async function toggleActive(id) {
    try {
        const res = await fetch(`${API_BASE}/toggle-active/${id}`, {
            method: "PUT",
            headers
        });

        if (!res.ok) throw new Error("Không thể thay đổi trạng thái!");
        await loadCategories();
    } catch (err) {
        console.error("❌ Lỗi toggle:", err);
        alert("Không thể thay đổi trạng thái!");
    }
}

// ==========================
// 8️⃣ Tìm kiếm danh mục
// ==========================
searchBtn.addEventListener("click", () => {
    const term = searchInput.value.trim();
    loadCategories(term);
});

// ==========================
// 9️⃣ Logout
// ==========================
document.getElementById("logoutBtn").addEventListener("click", () => {
    localStorage.removeItem("jwtToken");
    window.location.href = "/ui/login.html";
});

// ==========================
// 🔟 Khởi chạy ban đầu
// ==========================
loadCategories();
