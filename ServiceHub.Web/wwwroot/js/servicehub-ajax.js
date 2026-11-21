/**
 * ServiceHub AJAX & UI Utilities
 * Reusable helper module for modern fetch-based asynchronous operations,
 * CSRF protection, UI feedback, validation error rendering, and notifications.
 */
const ServiceHubAjax = (() => {
  /**
   * Retrieves the current Anti-Forgery Request Verification Token from the DOM.
   */
  function getAntiForgeryToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    if (input) {
      return input.value;
    }
    const meta = document.querySelector('meta[name="request-verification-token"]');
    return meta ? meta.content : "";
  }

  /**
   * Sets loading state on a button to prevent duplicate submissions.
   */
  function setButtonLoading(button, isLoading, loadingText = "") {
    if (!button) return;
    if (isLoading) {
      button.dataset.originalHtml = button.innerHTML;
      button.classList.add("is-loading");
      button.setAttribute("disabled", "disabled");
      if (loadingText) {
        button.setAttribute("aria-label", loadingText);
      }
    } else {
      button.classList.remove("is-loading");
      button.removeAttribute("disabled");
      if (button.dataset.originalHtml) {
        button.innerHTML = button.dataset.originalHtml;
      }
    }
  }

  /**
   * Shows a floating alert / toast notification.
   * @param {'success'|'danger'|'warning'|'info'|'error'} type 
   * @param {string} message 
   * @param {number} durationMs 
   */
  function showNotification(type, message, durationMs = 4500) {
    const normalizedType = type === "error" ? "danger" : type;
    const container = document.getElementById("servicehub-alert-container");
    if (!container) return;

    const alertEl = document.createElement("div");
    alertEl.className = `alert alert-${normalizedType} alert-dismissible fade show servicehub-toast mb-2 d-flex align-items-center shadow-sm`;
    alertEl.setAttribute("role", "alert");

    // Accessible SVGs per notification type
    let iconSvg = "";
    if (normalizedType === "success") {
      iconSvg = `<svg class="bi flex-shrink-0 me-2" width="18" height="18" viewBox="0 0 16 16" fill="currentColor">
        <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
      </svg>`;
    } else if (normalizedType === "danger") {
      iconSvg = `<svg class="bi flex-shrink-0 me-2" width="18" height="18" viewBox="0 0 16 16" fill="currentColor">
        <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/>
      </svg>`;
    } else {
      iconSvg = `<svg class="bi flex-shrink-0 me-2" width="18" height="18" viewBox="0 0 16 16" fill="currentColor">
        <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/>
      </svg>`;
    }

    alertEl.innerHTML = `
      ${iconSvg}
      <div class="flex-grow-1 text-break">${message}</div>
      <button type="button" class="btn-close ms-2" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    container.appendChild(alertEl);

    // Auto dismiss
    setTimeout(() => {
      alertEl.classList.remove("show");
      setTimeout(() => alertEl.remove(), 250);
    }, durationMs);
  }

  /**
   * Renders validation errors directly under form fields.
   */
  function displayFormErrors(formElement, errors) {
    clearFormErrors(formElement);
    if (!errors) return;

    let generalErrors = [];

    // Check if errors is an object or dictionary
    if (typeof errors === "object") {
      Object.keys(errors).forEach((key) => {
        const errorMessages = Array.isArray(errors[key]) ? errors[key] : [errors[key]];
        const messageText = errorMessages.join(" ");

        // Empty key or summary error
        if (!key || key === "" || key === "ModelOnly") {
          generalErrors.push(messageText);
          return;
        }

        // Find input by name or id (case-insensitive search)
        const inputs = Array.from(formElement.querySelectorAll("input, textarea, select"));
        const matchedInput = inputs.find(
          (input) => input.name.toLowerCase() === key.toLowerCase() || input.id.toLowerCase() === key.toLowerCase()
        );

        if (matchedInput) {
          matchedInput.classList.add("is-invalid");

          // Look for validation span or create one
          let span = formElement.querySelector(`[data-valmsg-for="${matchedInput.name}"]`) ||
                     matchedInput.closest(".mb-3")?.querySelector(".text-danger");

          if (span) {
            span.textContent = messageText;
            span.classList.remove("d-none");
          }
        } else {
          generalErrors.push(messageText);
        }
      });
    } else if (typeof errors === "string") {
      generalErrors.push(errors);
    }

    // Display general errors if any
    if (generalErrors.length > 0) {
      const summaryContainer = formElement.querySelector("[data-valmsg-summary]") ||
                               formElement.querySelector(".form-validation-summary");
      if (summaryContainer) {
        summaryContainer.innerHTML = generalErrors.map((msg) => `<div>${msg}</div>`).join("");
        summaryContainer.classList.remove("d-none");
      } else {
        showNotification("danger", generalErrors.join(" "));
      }
    }
  }

  /**
   * Clears all validation states from a form.
   */
  function clearFormErrors(formElement) {
    if (!formElement) return;

    formElement.querySelectorAll(".is-invalid").forEach((el) => {
      el.classList.remove("is-invalid");
    });

    formElement.querySelectorAll('[data-valmsg-for]').forEach((span) => {
      span.textContent = "";
    });

    const summaryContainer = formElement.querySelector("[data-valmsg-summary]") ||
                             formElement.querySelector(".form-validation-summary");
    if (summaryContainer) {
      summaryContainer.innerHTML = "";
      summaryContainer.classList.add("d-none");
    }
  }

  /**
   * Unified asynchronous fetch wrapper with security and error handling.
   */
  async function request(url, options = {}) {
    const defaultHeaders = {
      Accept: "application/json",
      "X-Requested-With": "XMLHttpRequest"
    };

    const token = getAntiForgeryToken();
    if (token) {
      defaultHeaders["RequestVerificationToken"] = token;
    }

    const submitBtn = options.button || null;
    if (submitBtn) {
      setButtonLoading(submitBtn, true);
    }

    let fetchOptions = {
      method: options.method || "GET",
      headers: { ...defaultHeaders, ...(options.headers || {}) }
    };

    if (options.body) {
      if (options.body instanceof FormData) {
        // FormData sets its own multipart Content-Type
        fetchOptions.body = options.body;
      } else if (typeof options.body === "object") {
        fetchOptions.headers["Content-Type"] = "application/json";
        fetchOptions.body = JSON.stringify(options.body);
      } else {
        fetchOptions.body = options.body;
      }
    }

    try {
      const response = await fetch(url, fetchOptions);

      // Handle 401 Unauthorized
      if (response.status === 401) {
        showNotification("warning", "Your session has expired. Redirecting to login...");
        setTimeout(() => {
          window.location.href = `/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
        }, 1200);
        return { success: false, status: 401, message: "Unauthorized" };
      }

      // Handle 403 Forbidden
      if (response.status === 403) {
        showNotification("danger", "You do not have permission to perform this action.");
        return { success: false, status: 403, message: "Forbidden" };
      }

      // Try parsing JSON response
      let data = null;
      const contentType = response.headers.get("content-type") || "";
      if (contentType.includes("application/json")) {
        data = await response.json();
      } else {
        const text = await response.text();
        data = { message: text };
      }

      // Handle 400 Bad Request (Validation or business error)
      if (response.status === 400) {
        return {
          success: false,
          status: 400,
          errors: data.errors || null,
          message: data.message || "Please correct the errors in the form."
        };
      }

      // Handle 404 Not Found
      if (response.status === 404) {
        showNotification("danger", data.message || "Requested resource was not found.");
        return { success: false, status: 404, message: data.message || "Not Found" };
      }

      // Handle other non-2xx statuses
      if (!response.ok) {
        const msg = data.message || "A server error occurred. Please try again.";
        showNotification("danger", msg);
        return { success: false, status: response.status, message: msg };
      }

      return { success: true, status: response.status, data };
    } catch (err) {
      console.error("ServiceHub AJAX network error:", err);
      showNotification("danger", "Network error. Please check your connection and try again.");
      return { success: false, status: 0, message: "Network connection error." };
    } finally {
      if (submitBtn) {
        setButtonLoading(submitBtn, false);
      }
    }
  }

  /**
   * Initializes password visibility toggle buttons.
   */
  function initPasswordToggles() {
    document.querySelectorAll(".password-toggle-btn").forEach((btn) => {
      if (btn.dataset.initialized) return;
      btn.dataset.initialized = "true";

      btn.addEventListener("click", (e) => {
        e.preventDefault();
        const input = btn.closest(".password-input-group")?.querySelector("input");
        if (!input) return;

        const isPassword = input.type === "password";
        input.type = isPassword ? "text" : "password";

        // Toggle icon SVG
        if (isPassword) {
          // Eye-slash icon (hide)
          btn.innerHTML = `<svg width="18" height="18" viewBox="0 0 16 16" fill="currentColor">
            <path d="m10.79 12.912-1.614-1.615a3.5 3.5 0 0 1-4.474-4.474l-2.06-2.06C.938 6.278 0 8 0 8s3 5.5 8 5.5a7.029 7.029 0 0 0 2.79-.588zM5.21 3.088A7.028 7.028 0 0 1 8 2.5c5 0 8 5.5 8 5.5s-.939 1.721-2.641 3.238l-2.062-2.062a3.5 3.5 0 0 0-4.474-4.474L5.21 3.089z"/>
            <path d="M5.525 7.646a2.5 2.5 0 0 0 2.829 2.829l-2.83-2.829zm4.95.708-2.829-2.83a2.5 2.5 0 0 1 2.829 2.829zm3.171-4.142-10 10-.708-.708 10-10 .708.708z"/>
          </svg>`;
          btn.setAttribute("title", "Hide password");
          btn.setAttribute("aria-label", "Hide password");
        } else {
          // Eye icon (show)
          btn.innerHTML = `<svg width="18" height="18" viewBox="0 0 16 16" fill="currentColor">
            <path d="M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8zM1.173 8a13.133 13.133 0 0 1 1.66-2.043C4.12 4.668 5.88 3.5 8 3.5c2.12 0 3.879 1.168 5.168 2.457A13.133 13.133 0 0 1 14.828 8c-.058.087-.122.183-.195.288-.335.48-.83 1.12-1.465 1.755C11.879 11.332 10.119 12.5 8 12.5c-2.12 0-3.879-1.168-5.168-2.457A13.134 13.134 0 0 1 1.172 8z"/>
            <path d="M8 5.5a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5zM4.5 8a3.5 3.5 0 1 1 7 0 3.5 3.5 0 0 1-7 0z"/>
          </svg>`;
          btn.setAttribute("title", "Show password");
          btn.setAttribute("aria-label", "Show password");
        }
      });
    });
  }

  // Self-init common DOM handlers on DOMContentLoaded
  document.addEventListener("DOMContentLoaded", () => {
    initPasswordToggles();
  });

  return {
    getAntiForgeryToken,
    setButtonLoading,
    showNotification,
    displayFormErrors,
    clearFormErrors,
    request,
    initPasswordToggles
  };
})();
