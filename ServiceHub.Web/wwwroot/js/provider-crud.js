/**
 * ServiceHub - Provider CRUD and Profile AJAX Management
 * Handles asynchronous operations for provider services and provider profile.
 */
document.addEventListener("DOMContentLoaded", () => {
  initProviderServicesCrud();
  initProviderProfileAjax();
});

/**
 * Initializes AJAX CRUD workflows for Provider Services.
 */
function initProviderServicesCrud() {
  const servicesTableBody = document.getElementById("services-table-body");
  const emptyStateCard = document.getElementById("services-empty-state");
  const tableContainer = document.getElementById("services-table-container");

  // --- Add Service Modal & Form ---
  const addModalEl = document.getElementById("addServiceModal");
  const addForm = document.getElementById("addServiceForm");
  const addModal = addModalEl && typeof bootstrap !== "undefined" ? new bootstrap.Modal(addModalEl) : null;

  if (addForm) {
    addForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      ServiceHubAjax.clearFormErrors(addForm);

      const submitBtn = addForm.querySelector('button[type="submit"]');
      const formData = new FormData(addForm);

      const result = await ServiceHubAjax.request("/ServiceProvider/Services/CreateAjax", {
        method: "POST",
        body: formData,
        button: submitBtn
      });

      if (result.success) {
        ServiceHubAjax.showNotification("success", result.data.message || "Service added successfully!");
        addForm.reset();
        if (addModal) addModal.hide();
        await reloadServicesList();
      } else if (result.status === 400 && result.errors) {
        ServiceHubAjax.displayFormErrors(addForm, result.errors);
      }
    });

    if (addModalEl) {
      addModalEl.addEventListener("hidden.bs.modal", () => {
        ServiceHubAjax.clearFormErrors(addForm);
        addForm.reset();
      });
    }
  }

  // --- Edit Service Modal & Form ---
  const editModalEl = document.getElementById("editServiceModal");
  const editForm = document.getElementById("editServiceForm");
  const editModal = editModalEl && typeof bootstrap !== "undefined" ? new bootstrap.Modal(editModalEl) : null;

  if (editForm) {
    editForm.addEventListener("submit", async (e) => {
      e.preventDefault();
      ServiceHubAjax.clearFormErrors(editForm);

      const serviceId = editForm.querySelector('input[name="Id"]').value;
      const submitBtn = editForm.querySelector('button[type="submit"]');
      const formData = new FormData(editForm);

      const result = await ServiceHubAjax.request(`/ServiceProvider/Services/EditAjax/${serviceId}`, {
        method: "POST",
        body: formData,
        button: submitBtn
      });

      if (result.success) {
        ServiceHubAjax.showNotification("success", result.data.message || "Service updated successfully!");
        if (editModal) editModal.hide();
        await reloadServicesList();
      } else if (result.status === 400 && result.errors) {
        ServiceHubAjax.displayFormErrors(editForm, result.errors);
      }
    });

    if (editModalEl) {
      editModalEl.addEventListener("hidden.bs.modal", () => {
        ServiceHubAjax.clearFormErrors(editForm);
      });
    }
  }

  // --- Delete Confirmation Modal ---
  const deleteModalEl = document.getElementById("deleteServiceModal");
  const confirmDeleteBtn = document.getElementById("confirmDeleteBtn");
  const deleteServiceNameEl = document.getElementById("deleteServiceNameSpan");
  const deleteModal = deleteModalEl && typeof bootstrap !== "undefined" ? new bootstrap.Modal(deleteModalEl) : null;
  let serviceIdToDelete = null;

  if (confirmDeleteBtn) {
    confirmDeleteBtn.addEventListener("click", async () => {
      if (!serviceIdToDelete) return;

      const result = await ServiceHubAjax.request(`/ServiceProvider/Services/DeleteAjax/${serviceIdToDelete}`, {
        method: "POST",
        button: confirmDeleteBtn
      });

      if (result.success) {
        ServiceHubAjax.showNotification("success", result.data.message || "Service deleted successfully.");
        if (deleteModal) deleteModal.hide();
        // Remove row from table directly
        const row = document.getElementById(`service-row-${serviceIdToDelete}`);
        if (row) {
          row.style.transition = "opacity 0.3s";
          row.style.opacity = "0";
          setTimeout(() => {
            row.remove();
            checkEmptyState();
          }, 300);
        } else {
          await reloadServicesList();
        }
      }
    });
  }

  // Delegate click for Edit and Delete buttons on table rows
  document.addEventListener("click", async (e) => {
    // Edit Click
    const editBtn = e.target.closest(".btn-ajax-edit");
    if (editBtn) {
      e.preventDefault();
      const id = editBtn.dataset.serviceId;
      await openEditModal(id);
      return;
    }

    // Delete Click
    const deleteBtn = e.target.closest(".btn-ajax-delete");
    if (deleteBtn) {
      e.preventDefault();
      serviceIdToDelete = deleteBtn.dataset.serviceId;
      const serviceName = deleteBtn.dataset.serviceName || "this service";
      if (deleteServiceNameEl) {
        deleteServiceNameEl.textContent = serviceName;
      }
      if (deleteModal) {
        deleteModal.show();
      }
    }
  });

  /**
   * Loads a service by ID and populates the edit modal.
   */
  async function openEditModal(id) {
    if (!editForm) return;

    ServiceHubAjax.clearFormErrors(editForm);
    const result = await ServiceHubAjax.request(`/ServiceProvider/Services/Data/${id}`);
    if (result.success && result.data.service) {
      const s = result.data.service;
      editForm.querySelector('input[name="Id"]').value = s.id;
      editForm.querySelector('input[name="ServiceName"]').value = s.serviceName;
      editForm.querySelector('textarea[name="Description"]').value = s.description || "";
      editForm.querySelector('input[name="Price"]').value = s.price;
      editForm.querySelector('input[name="EstimatedDurationMinutes"]').value = s.estimatedDurationMinutes;
      editForm.querySelector('input[name="IsActive"]').checked = s.isActive;

      if (editModal) {
        editModal.show();
      }
    }
  }

  /**
   * Asynchronously reloads the services table via JSON endpoint.
   */
  async function reloadServicesList() {
    if (!servicesTableBody) return;

    // Show loading row
    servicesTableBody.innerHTML = `
      <tr>
        <td colspan="5" class="text-center py-4 text-muted">
          <div class="spinner-border spinner-border-sm text-primary me-2" role="status"></div>
          Loading services...
        </td>
      </tr>
    `;

    const result = await ServiceHubAjax.request("/ServiceProvider/Services/Data");
    if (result.success && result.data.services) {
      const services = result.data.services;
      renderServicesTable(services);
    }
  }

  /**
   * Renders the services rows into the table.
   */
  function renderServicesTable(services) {
    if (!servicesTableBody) return;

    if (!services || services.length === 0) {
      servicesTableBody.innerHTML = "";
      if (tableContainer) tableContainer.classList.add("d-none");
      if (emptyStateCard) emptyStateCard.classList.remove("d-none");
      return;
    }

    if (tableContainer) tableContainer.classList.remove("d-none");
    if (emptyStateCard) emptyStateCard.classList.add("d-none");

    servicesTableBody.innerHTML = services.map((s) => `
      <tr id="service-row-${s.id}">
        <th scope="row" class="fw-semibold">
          <div>${escapeHtml(s.serviceName)}</div>
          ${s.description ? `<small class="text-muted text-truncate d-block" style="max-width: 280px;">${escapeHtml(s.description)}</small>` : ""}
        </th>
        <td>$${s.priceFormatted}</td>
        <td>${s.estimatedDurationMinutes} min</td>
        <td>
          <span class="badge ${s.isActive ? "text-bg-success" : "text-bg-secondary"}">
            ${s.isActive ? "Active" : "Inactive"}
          </span>
        </td>
        <td class="text-end">
          <div class="btn-group btn-group-sm" role="group">
            <a href="/ServiceProvider/Services/${s.id}" class="btn btn-outline-secondary">Details</a>
            <button type="button" class="btn btn-outline-primary btn-ajax-edit" data-service-id="${s.id}">Edit</button>
            <button type="button" class="btn btn-outline-danger btn-ajax-delete" data-service-id="${s.id}" data-service-name="${escapeHtml(s.serviceName)}">Delete</button>
          </div>
        </td>
      </tr>
    `).join("");
  }

  function checkEmptyState() {
    if (!servicesTableBody) return;
    const remainingRows = servicesTableBody.querySelectorAll("tr");
    if (remainingRows.length === 0) {
      if (tableContainer) tableContainer.classList.add("d-none");
      if (emptyStateCard) emptyStateCard.classList.remove("d-none");
    }
  }
}

/**
 * Initializes AJAX form handling for the Provider Profile page/card.
 */
function initProviderProfileAjax() {
  const profileForm = document.getElementById("providerProfileForm");
  if (!profileForm) return;

  profileForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    ServiceHubAjax.clearFormErrors(profileForm);

    const submitBtn = profileForm.querySelector('button[type="submit"]');
    const formData = new FormData(profileForm);

    const result = await ServiceHubAjax.request("/ServiceProvider/Profile/Update", {
      method: "POST",
      body: formData,
      button: submitBtn
    });

    if (result.success) {
      ServiceHubAjax.showNotification("success", result.data.message || "Profile updated successfully!");

      // Update header/nav displays dynamically if present
      if (result.data.data?.fullName) {
        document.querySelectorAll(".profile-display-name").forEach((el) => {
          el.textContent = result.data.data.fullName;
        });
      }
    } else if (result.status === 400 && result.errors) {
      ServiceHubAjax.displayFormErrors(profileForm, result.errors);
    }
  });
}

/**
 * Sanitizes strings for safe HTML interpolation.
 */
function escapeHtml(unsafe) {
  if (!unsafe) return "";
  return unsafe
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
