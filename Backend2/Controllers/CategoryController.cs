using Backend2.Models;
using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.Json;
using Backend2.Services;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISystemAccountRepository _accountRepository;
        private readonly IAuditLogService _auditLogService;

        public CategoryController(ICategoryRepository categoryRepository, ISystemAccountRepository accountRepository,IAuditLogService auditLogService)
        {
            _categoryRepository = categoryRepository;
            _accountRepository = accountRepository;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<Category>> GetCategories([FromQuery] string? search)
        {
            var categories = _categoryRepository.GetCategories(search);
            return Ok(categories);
        }

        [HttpGet("with-count")]
        public ActionResult<IEnumerable<CategoryWithArticleCount>> GetCategoriesWithArticleCount([FromQuery] string? search = "")
        {
            var categories = _categoryRepository.GetCategoriesWithArticleCount(search);
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public ActionResult<Category> GetCategoryById(int id)
        {
            var category = _categoryRepository.GetCategoryById(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (string.IsNullOrEmpty(category.CategoryName) || string.IsNullOrEmpty(category.CategoryDescription))
            {
                return BadRequest(new { message = "Category name and description are mandatory." });
            }
            if (_categoryRepository.IsCategoryNameExist(category.CategoryName))
            {
                return BadRequest(new { message = "Category name already exists." });
            }
            _categoryRepository.AddCategory(category);
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";

            await _auditLogService.LogAsync(
                   userEmail,
                   "Create",
                   "Category",
                   JsonSerializer.Serialize(new { category.CategoryId }),
                   null, 
                   category         
               );
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryId }, category);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingCategory = _categoryRepository.GetCategoryById(id);
            if (existingCategory == null)
            {
                return NotFound(new { message = "Category not found." });
            }

        
            var oldValuesForLog = new
            {
                existingCategory.CategoryId,
                existingCategory.CategoryName,
                existingCategory.CategoryDescription,
                existingCategory.ParentCategoryId,
                existingCategory.IsActive
            };
          
            if (existingCategory.ParentCategoryId != dto.ParentCategoryId && _categoryRepository.HasNewsArticles(id))
            {
                return BadRequest(new { message = "Cannot change ParentCategoryID because this category is used by articles." });
            }
            if (_categoryRepository.IsCategoryNameExist(dto.CategoryName, id))
            {
                return BadRequest(new { message = "Category name already exists." });
            }

            try
            {
                existingCategory.CategoryName = dto.CategoryName;
                existingCategory.CategoryDescription = dto.CategoryDescription;
                existingCategory.ParentCategoryId = dto.ParentCategoryId;
                existingCategory.IsActive = dto.IsActive ?? existingCategory.IsActive;

                _categoryRepository.UpdateCategory(existingCategory);

                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                await _auditLogService.LogAsync(
                    userEmail,
                    "Update",
                    "Category",
                    JsonSerializer.Serialize(new { CategoryId = id }),
                    oldValuesForLog,     
                    existingCategory     
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating category: {ex.Message}" });
            }
        }
        [HttpPut("toggle-active/{id}")]
        public IActionResult ToggleActive(int id)
        {
            var category = _categoryRepository.GetCategoryById(id);
            if (category == null)
            {
                return NotFound();
            }
            _categoryRepository.ToggleActive(id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (_categoryRepository.HasNewsArticles(id))
            {
                return BadRequest(new { message = "Cannot delete category as it is used by news articles." });
            }

            try
            {
                var categoryToDelete = _categoryRepository.GetCategoryById(id);
                if (categoryToDelete == null)
                {
                   
                    return NotFound(new { message = "Category not found." });
                }

                var oldValuesForLog = new
                {
                    categoryToDelete.CategoryId,
                    categoryToDelete.CategoryName,
                    categoryToDelete.CategoryDescription,
                    categoryToDelete.ParentCategoryId,
                    categoryToDelete.IsActive
                };

                _categoryRepository.DeleteCategory(id); 

                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                await _auditLogService.LogAsync(
                    userEmail,
                    "Delete",
                    "Category",
                    JsonSerializer.Serialize(new { CategoryId = id }),
                    oldValuesForLog,
                    null            
                );

                return NoContent(); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting category: {ex.Message}" });
            }
        }
    }
}
