// File: Backend2/Controllers/TagController.cs

using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 // <-- 1. Add Service using
using Backend2.ViewModels;         // <-- 2. Add DTO using
using System.Security.Claims;      // <-- 3. Add Claims using
using System.Text.Json;            // <-- 4. Add Json using
using System.Threading.Tasks;
using Backend2.Services;      // <-- 5. Add Task using

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class TagController : ControllerBase
    {
        private readonly ITagRepository _tagRepository;
        private readonly IAuditLogService _auditLogService; // <-- 6. Declare Service

        // 7. Inject Service in Constructor
        public TagController(ITagRepository tagRepository, IAuditLogService auditLogService)
        {
            _tagRepository = tagRepository;
            _auditLogService = auditLogService; // <-- Assign Service
        }

        // --- GetTags (No changes needed for basic list) ---
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<Tag>> GetTags([FromQuery] string? search)
        {
            var tags = _tagRepository.GetTags(search);
            return Ok(tags);
        }

        // --- GetTagById (No changes needed) ---
        [HttpGet("{id}")]
        public ActionResult<Tag> GetTagById(int id)
        {
            var tag = _tagRepository.GetTagById(id);
            if (tag == null)
            {
                return NotFound();
            }
            return Ok(tag);
        }

        // --- UPDATE CreateTag ---
        [HttpPost]
        // Make async for Audit Logging
        public async Task<IActionResult> CreateTag([FromBody] TagCreateUpdateDto dto)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(dto.TagName))
            {
                return BadRequest(new { message = "Tag name cannot be empty." });
            }

            // Check for duplicate name
            if (_tagRepository.IsTagNameExist(dto.TagName))
            {
                return BadRequest(new { message = "Tag name already exists." });
            }

            // Create the Tag model from DTO
            var newTag = new Tag
            {
                TagName = dto.TagName,
                 Note=dto.Note
            };

            try
            {
                // Call Repository
                _tagRepository.AddTag(newTag);
                // After AddTag, newTag.TagId should be populated by EF

                // Add Audit Log
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                await _auditLogService.LogAsync(
                    userEmail,
                    "Create",
                    "Tag",
                    JsonSerializer.Serialize(new { newTag.TagId }),
                    null, // Old values are null for create
                    newTag // Log the newly created Tag object
                );

                // Return CreatedAtAction with the full Tag object
                return CreatedAtAction(nameof(GetTagById), new { id = newTag.TagId }, newTag);
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                return StatusCode(500, new { message = $"Error creating tag: {ex.Message}" });
            }
        }

        // --- UPDATE UpdateTag ---
        [HttpPut("{id}")]
        // Make async for Audit Logging
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagCreateUpdateDto dto)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(dto.TagName))
            {
                return BadRequest(new { message = "Tag name cannot be empty." });
            }

            // Check if tag exists
            var existingTag = _tagRepository.GetTagById(id);
            if (existingTag == null)
            {
                return NotFound(new { message = "Tag not found." });
            }

            // Create snapshot for logging BEFORE updating
            var oldValuesForLog = new { existingTag.TagId, existingTag.TagName };

            // Check for duplicate name (excluding the current tag)
            if (_tagRepository.IsTagNameExist(dto.TagName, id))
            {
                return BadRequest(new { message = "Tag name already exists." });
            }

            try
            {
                // Update the existing tag entity
                existingTag.TagName = dto.TagName;
                existingTag.Note = dto.Note;
                // Call Repository
                _tagRepository.UpdateTag(existingTag);

                // Add Audit Log
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                await _auditLogService.LogAsync(
                    userEmail,
                    "Update",
                    "Tag",
                    JsonSerializer.Serialize(new { TagId = id }),
                    oldValuesForLog, // Log the simple snapshot
                    existingTag      // Log the updated entity
                );

                return NoContent(); // Success
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                return StatusCode(500, new { message = $"Error updating tag: {ex.Message}" });
            }
        }

        // --- UPDATE DeleteTag ---
        [HttpDelete("{id}")]
        // Make async for Audit Logging
        public async Task<IActionResult> DeleteTag(int id)
        {
            // Check if tag exists (needed for logging)
            var tagToDelete = _tagRepository.GetTagById(id);
            if (tagToDelete == null)
            {
                return NotFound(new { message = "Tag not found." });
            }

            // Check if referenced (keep this rule)
            if (_tagRepository.IsTagReferencedInNews(id))
            {
                return BadRequest(new { message = "Cannot delete this tag. It is currently used by one or more news articles." });
            }

            // Create snapshot for logging BEFORE deleting
            var oldValuesForLog = new { tagToDelete.TagId, tagToDelete.TagName };

            try
            {
                // Call Repository
                _tagRepository.DeleteTag(id);

                // Add Audit Log
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                await _auditLogService.LogAsync(
                    userEmail,
                    "Delete",
                    "Tag",
                    JsonSerializer.Serialize(new { TagId = id }),
                    oldValuesForLog, // Log the snapshot before deletion
                    null             // No new values after deletion
                );

                return NoContent(); // Success
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                return StatusCode(500, new { message = $"Error deleting tag: {ex.Message}" });
            }
        }
    }
}