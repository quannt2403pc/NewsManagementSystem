const API_BASE = "https://localhost:7134/api/Tag";

const token = localStorage.getItem("jwtToken");

if (!token) {
    alert("Vui lòng đăng nhập lại!");
    window.location.href = "login.html";
}

const headers = {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
};

const tagTableBody = document.getElementById('tagTableBody');
const addTagBtn = document.getElementById('addTagBtn');
const tagModal = new bootstrap.Modal(document.getElementById('tagModal'));
const modalTitle = document.getElementById('modalTitle');
const tagForm = document.getElementById('tagForm');
const tagIdInput = document.getElementById('tagId');
const tagNameInput = document.getElementById('tagName');
const tagNoteInput = document.getElementById('tagNote');
const searchInput = document.getElementById('searchInput');
const searchBtn = document.getElementById('searchBtn');
const logoutBtn = document.getElementById('logoutBtn');

const fetchTags = async (search = '') => {
    try {
        const url = search ? `${API_BASE}?search=${search}` : API_BASE;
        const response = await fetch(url, { headers });

        if (response.status === 401 || response.status === 403) {
            alert('Bạn không có quyền truy cập. Đang chuyển hướng đến trang đăng nhập.');
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorData = await response.json();
            alert(errorData.message || 'Lỗi khi lấy dữ liệu.');
            return;
        }

        const tags = await response.json(); // ✅ Use data directly
        renderTable(tags);
    } catch (error) {
        console.error('Lỗi khi lấy thẻ:', error);
        alert('Đã xảy ra lỗi khi lấy dữ liệu thẻ.');
    }
};

const renderTable = (tags) => {
    tagTableBody.innerHTML = '';
    if (tags.length === 0) {
        tagTableBody.innerHTML = '<tr><td colspan="4" class="text-center">Không tìm thấy thẻ nào.</td></tr>';
        return;
    }

    tags.forEach(tag => {
        const row = `
            <tr>
                <td>${tag.tagId}</td>
                <td>${tag.tagName}</td>
                <td>${tag.note || '—'}</td>
                <td>
                    <button class="btn btn-warning btn-sm edit-btn" data-id="${tag.tagId}">Sửa</button>
                    <button class="btn btn-danger btn-sm delete-btn" data-id="${tag.tagId}">Xóa</button>
                </td>
            </tr>
        `;
        tagTableBody.innerHTML += row;
    });

    document.querySelectorAll('.edit-btn').forEach(btn => btn.addEventListener('click', handleEdit));
    document.querySelectorAll('.delete-btn').forEach(btn => btn.addEventListener('click', handleDelete));
};

const handleEdit = async (e) => {
    const id = e.target.dataset.id;
    try {
        const response = await fetch(`${API_BASE}/${id}`, { headers });
        if (!response.ok) throw new Error('Không thể lấy thẻ để sửa.');
        const tag = await response.json();

        modalTitle.textContent = 'Sửa Thẻ';
        tagIdInput.value = tag.tagId;
        tagNameInput.value = tag.tagName;
        tagNoteInput.value = tag.note || '';
        tagModal.show();
    } catch (error) {
        console.error('Lỗi khi sửa thẻ:', error);
        alert('Không thể tải dữ liệu thẻ để sửa.');
    }
};

const handleDelete = async (e) => {
    const id = e.target.dataset.id;
    if (confirm('Bạn có chắc chắn muốn xóa thẻ này không?')) {
        try {
            const response = await fetch(`${API_BASE}/${id}`, { method: 'DELETE', headers });
            if (!response.ok) {
                const error = await response.json();
                alert(error.message || 'Xóa thẻ thất bại.');
                return;
            }
            alert('Xóa thẻ thành công!');
            fetchTags();
        } catch (error) {
            console.error('Lỗi khi xóa thẻ:', error);
            alert('Đã xảy ra lỗi trong quá trình xóa.');
        }
    }
};

const handleFormSubmit = async (e) => {
    e.preventDefault();
    const id = tagIdInput.value;
    const tag = {
        tagName: tagNameInput.value,
        note: tagNoteInput.value
    };

    try {
        let response;
        if (id) {
            tag.tagId = parseInt(id);
            response = await fetch(`${API_BASE}/${id}`, {
                method: 'PUT',
                headers,
                body: JSON.stringify(tag)
            });
        } else {
            response = await fetch(API_BASE, {
                method: 'POST',
                headers,
                body: JSON.stringify(tag)
            });
        }

        if (!response.ok) {
            const error = await response.json();
            alert(error.message || 'Thao tác thất bại.');
            return;
        }

        tagModal.hide();
        fetchTags();
    } catch (error) {
        console.error('Lỗi khi lưu thẻ:', error);
        alert('Đã xảy ra lỗi khi lưu thẻ.');
    }
};

addTagBtn.addEventListener('click', () => {
    tagForm.reset();
    modalTitle.textContent = 'Thêm Thẻ mới';
    tagIdInput.value = '';
    tagModal.show();
});

searchBtn.addEventListener('click', () => {
    const search = searchInput.value;
    fetchTags(search);
});

tagForm.addEventListener('submit', handleFormSubmit);

logoutBtn.addEventListener('click', () => {
    localStorage.removeItem("jwtToken");
    window.location.href = "login.html";
});

fetchTags();