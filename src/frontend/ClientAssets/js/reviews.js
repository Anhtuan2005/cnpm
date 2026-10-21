function initReviewForm() {
  var form = document.getElementById("reviewForm");
  if (!form) return;
  form.addEventListener("submit", function (event) {
    event.preventDefault();
    fetch("/Review/Submit", {
      method: "POST",
      headers: { "RequestVerificationToken": antiForgeryToken() },
      body: new FormData(form)
    })
      .then(function (response) {
        if (!response.ok) throw new Error("Không thể gửi đánh giá.");
        return response.json();
      })
      .then(function (data) {
        if (!data.success) {
          showToast(data.message, "error");
          return;
        }
        appendReview(data.review, antiForgeryToken());
        updateReviewStats(data.stats);
        form.remove();
        refreshIcons();
        applyReviewFilter();
        showToast(data.message, "success");
      })
      .catch(function () { showToast("Không thể gửi đánh giá. Vui lòng thử lại.", "error"); });
  });
}

function appendReview(review, token) {
  var list = document.getElementById("reviewList");
  if (!list) return;
  var empty = document.querySelector("[data-review-empty]");
  if (empty) empty.remove();
  var item = document.createElement("article");
  item.className = "review-item new";
  item.dataset.reviewItem = "";
  item.dataset.rating = String(review.rating);
  var initial = review.user ? review.user.substring(0, 1).toUpperCase() : "K";
  var stars = '<span class="star-meter" aria-hidden="true">';
  for (var i = 1; i <= 5; i++) {
    stars += '<span class="star-meter-star" style="--fill:' + (i <= review.rating ? 100 : 0) + '%"><span>★</span></span>';
  }
  stars += "</span>";
  item.innerHTML =
    '<div class="review-avatar">' + escapeHtml(initial) + "</div>" +
    '<div class="review-copy">' +
    '<div class="review-meta"><strong>' + escapeHtml(review.user) + "</strong><span>" + escapeHtml(review.date) + "</span></div>" +
    stars +
    '<p data-review-comment>' + escapeHtml(review.comment) + "</p>" +
    renderReviewImages(review.images) +
    renderReviewManagement(review.id, review.rating, review.comment, token) +
    "</div>";
  item.dataset.reviewId = String(review.id);
  list.prepend(item);
}

function renderReviewManagement(reviewId, rating, comment, token) {
  if (!reviewId) return "";
  var id = String(reviewId);
  var stars = "";
  for (var star = 5; star >= 1; star--) {
    var inputId = "review-" + id + "-edit-star-" + star;
    stars += '<input id="' + inputId + '" type="radio" name="rating" value="' + star + '"' + (Number(rating) === star ? " checked" : "") + ' required>' +
      '<label for="' + inputId + '" aria-label="' + star + ' sao">★</label>';
  }
  return '<div class="review-management" data-review-management>' +
    '<div class="review-action-row">' +
    '<button class="review-action-button" type="button" data-review-edit-toggle aria-expanded="false">Chỉnh sửa</button>' +
    '<button class="review-action-button is-danger" type="button" data-review-delete-toggle aria-expanded="false">Xóa đánh giá</button>' +
    '</div>' +
    '<form class="review-edit-form" data-review-edit-form hidden>' +
    '<input type="hidden" name="__RequestVerificationToken" value="' + escapeHtml(token || "") + '">' +
    '<input type="hidden" name="reviewId" value="' + escapeHtml(id) + '">' +
    '<fieldset class="review-edit-rating"><legend>Điều chỉnh số sao</legend><div class="star-picker review-edit-stars">' + stars + '</div></fieldset>' +
    '<label class="review-edit-label" for="review-' + id + '-comment">Nhận xét</label>' +
    '<textarea id="review-' + id + '-comment" name="comment" rows="3" minlength="10" maxlength="1000" required data-review-edit-comment>' + escapeHtml(comment) + '</textarea>' +
    '<small class="review-edit-note">Ảnh đã gửi được giữ nguyên khi chỉnh sửa.</small>' +
    '<div class="review-edit-actions"><button class="btn-primary" type="submit">Lưu thay đổi</button><button class="btn-ghost" type="button" data-review-edit-cancel>Hủy</button></div>' +
    '</form>' +
    '<div class="review-delete-confirm" data-review-delete-confirmation hidden>' +
    '<span>Xóa đánh giá này? Thao tác này không thể hoàn tác.</span>' +
    '<div><button class="review-delete-yes" type="button" data-review-delete-confirm>Xác nhận xóa</button><button class="review-delete-no" type="button" data-review-delete-cancel>Hủy</button></div>' +
    '</div>' +
    '</div>';
}

function initReviewManagement() {
  var list = document.getElementById("reviewList");
  if (!list) return;

  list.addEventListener("click", function (event) {
    var editToggle = event.target.closest("[data-review-edit-toggle]");
    if (editToggle) {
      var card = editToggle.closest("[data-review-item]");
      var form = card && card.querySelector("[data-review-edit-form]");
      var confirmation = card && card.querySelector("[data-review-delete-confirmation]");
      if (!form) return;
      confirmation.hidden = true;
      card.querySelector("[data-review-delete-toggle]").setAttribute("aria-expanded", "false");
      var open = form.hidden;
      form.hidden = !open;
      editToggle.setAttribute("aria-expanded", String(open));
      if (open) {
        resetReviewEditForm(card, form);
        form.querySelector("[data-review-edit-comment]").focus();
      }
      return;
    }

    var cancelEdit = event.target.closest("[data-review-edit-cancel]");
    if (cancelEdit) {
      var editForm = cancelEdit.closest("[data-review-edit-form]");
      var editCard = editForm.closest("[data-review-item]");
      resetReviewEditForm(editCard, editForm);
      editForm.hidden = true;
      editCard.querySelector("[data-review-edit-toggle]").setAttribute("aria-expanded", "false");
      return;
    }

    var deleteToggle = event.target.closest("[data-review-delete-toggle]");
    if (deleteToggle) {
      var deleteCard = deleteToggle.closest("[data-review-item]");
      var deleteForm = deleteCard.querySelector("[data-review-edit-form]");
      var deletePanel = deleteCard.querySelector("[data-review-delete-confirmation]");
      deleteForm.hidden = true;
      deleteCard.querySelector("[data-review-edit-toggle]").setAttribute("aria-expanded", "false");
      deletePanel.hidden = !deletePanel.hidden;
      deleteToggle.setAttribute("aria-expanded", String(!deletePanel.hidden));
      return;
    }

    var cancelDelete = event.target.closest("[data-review-delete-cancel]");
    if (cancelDelete) {
      var cancelCard = cancelDelete.closest("[data-review-item]");
      cancelCard.querySelector("[data-review-delete-confirmation]").hidden = true;
      cancelCard.querySelector("[data-review-delete-toggle]").setAttribute("aria-expanded", "false");
      return;
    }

    var confirmDelete = event.target.closest("[data-review-delete-confirm]");
    if (confirmDelete) deleteReview(confirmDelete);
  });

  list.addEventListener("submit", function (event) {
    var form = event.target.closest("[data-review-edit-form]");
    if (!form) return;
    event.preventDefault();
    saveReview(form);
  });
}

function resetReviewEditForm(card, form) {
  var comment = card.querySelector("[data-review-comment]");
  var commentInput = form.querySelector("[data-review-edit-comment]");
  if (comment && commentInput) commentInput.value = comment.textContent;
  form.querySelectorAll('input[name="rating"]').forEach(function (input) {
    input.checked = input.value === card.dataset.rating;
  });
}

function saveReview(form) {
  var card = form.closest("[data-review-item]");
  var submit = form.querySelector('[type="submit"]');
  submit.disabled = true;
  fetch("/Review/Edit", {
    method: "POST",
    headers: { "RequestVerificationToken": antiForgeryToken() },
    body: new FormData(form)
  })
    .then(readReviewResponse)
    .then(function (data) {
      if (!data.success) {
        showToast(data.message || "Không thể cập nhật đánh giá.", "error");
        return;
      }
      card.dataset.rating = String(data.review.rating);
      card.querySelector("[data-review-comment]").textContent = data.review.comment;
      card.querySelectorAll(".star-meter-star").forEach(function (star, index) {
        star.style.setProperty("--fill", index + 1 <= data.review.rating ? "100%" : "0%");
      });
      updateReviewStats(data.stats);
      form.hidden = true;
      card.querySelector("[data-review-edit-toggle]").setAttribute("aria-expanded", "false");
      applyReviewFilter();
      showToast(data.message, "success");
    })
    .catch(function () { showToast("Không thể cập nhật đánh giá. Vui lòng thử lại.", "error"); })
    .finally(function () { submit.disabled = false; });
}

function deleteReview(button) {
  var card = button.closest("[data-review-item]");
  var form = card.querySelector("[data-review-edit-form]");
  var reviewId = card.dataset.reviewId;
  button.disabled = true;
  var body = new FormData(form);
  body.set("reviewId", reviewId);
  fetch("/Review/Delete", {
    method: "POST",
    headers: { "RequestVerificationToken": antiForgeryToken() },
    body: body
  })
    .then(readReviewResponse)
    .then(function (data) {
      if (!data.success) {
        showToast(data.message || "Không thể xóa đánh giá.", "error");
        return;
      }
      card.remove();
      updateReviewStats(data.stats);
      ensureReviewEmptyState();
      applyReviewFilter();
      showToast(data.message, "success");
    })
    .catch(function () { showToast("Không thể xóa đánh giá. Vui lòng thử lại.", "error"); })
    .finally(function () { button.disabled = false; });
}

function readReviewResponse(response) {
  return response.json().then(function (data) {
    if (!response.ok) throw new Error(data.message || "Không thể cập nhật đánh giá.");
    return data;
  });
}

function updateReviewStats(stats) {
  if (!stats) return;
  var count = Number(stats.count || 0);
  var average = Number(stats.average || 0);
  var distribution = stats.distribution || {};
  var photoLabel = document.querySelector("[data-review-photo-count]");
  var roundedAverage = Math.round(average * 2) / 2;
  document.querySelectorAll("[data-review-average]").forEach(function (label) {
    label.textContent = count === 0
      ? (label.dataset.reviewAverageEmpty || "0/5 sao")
      : new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 1 }).format(average) + "/5 sao";
  });
  document.querySelectorAll("[data-review-count]").forEach(function (label) {
    label.textContent = count + " lượt đánh giá";
  });
  document.querySelectorAll("[data-review-total]").forEach(function (label) {
    label.textContent = count + " đánh giá";
  });
  if (photoLabel) photoLabel.textContent = Number(stats.photoCount || 0) + " ảnh thực tế";
  document.querySelectorAll("[data-review-average-stars] .star-meter-star").forEach(function (star, index) {
    star.style.setProperty("--fill", Math.max(0, Math.min(100, (roundedAverage - index) * 100)) + "%");
  });

  var allCount = document.querySelector('[data-review-filter-count="all"]');
  if (allCount) allCount.textContent = String(count);
  for (var star = 1; star <= 5; star++) {
    var starCount = Number(distribution[String(star)] || 0);
    var filterCount = document.querySelector('[data-review-filter-count="' + star + '"]');
    var progress = document.querySelector('[data-review-progress="' + star + '"]');
    var percent = document.querySelector('[data-review-percent="' + star + '"]');
    var pct = count === 0 ? 0 : Math.floor(starCount * 100 / count);
    if (filterCount) filterCount.textContent = String(starCount);
    if (progress) progress.value = pct;
    if (percent) percent.textContent = pct + "%";
  }
}

function ensureReviewEmptyState() {
  var list = document.getElementById("reviewList");
  if (!list || list.querySelector("[data-review-item]") || list.querySelector("[data-review-empty]")) return;
  var empty = document.createElement("div");
  empty.className = "review-empty";
  empty.dataset.reviewEmpty = "";
  var icon = document.createElement("i");
  icon.setAttribute("data-lucide", "message-square-plus");
  icon.setAttribute("aria-hidden", "true");
  var heading = document.createElement("strong");
  heading.textContent = "Chưa có đánh giá";
  var description = document.createElement("span");
  description.textContent = "Đánh giá sau khi mua sẽ giúp khách khác chọn điện thoại phù hợp.";
  empty.append(icon, heading, description);
  list.appendChild(empty);
  refreshIcons();
}

function renderReviewImages(images) {
  if (!images || !images.length) return "";
  var html = '<div class="review-photo-grid">';
  images.forEach(function (url) {
    html += '<a class="review-photo-thumb" href="' + escapeHtml(url) + '" target="_blank" rel="noopener">' +
      '<img src="' + escapeHtml(url) + '" alt="Ảnh đánh giá" loading="lazy" decoding="async" />' +
      "</a>";
  });
  return html + "</div>";
}

function initReviewImagePreview() {
  var input = document.querySelector("[data-review-images]");
  var preview = document.querySelector("[data-review-preview]");
  if (!input || !preview) return;

  input.addEventListener("change", function () {
    preview.innerHTML = "";
    var files = Array.prototype.slice.call(input.files || []);
    if (files.length > 4) {
      showToast("Bạn chỉ có thể tải tối đa 4 ảnh cho mỗi đánh giá.", "error");
      input.value = "";
      return;
    }

    files.forEach(function (file) {
      var item = document.createElement("span");
      var image = document.createElement("img");
      var url = URL.createObjectURL(file);
      image.src = url;
      image.alt = file.name;
      image.decoding = "async";
      image.onload = function () { URL.revokeObjectURL(url); };
      item.appendChild(image);
      preview.appendChild(item);
    });
  });
}

