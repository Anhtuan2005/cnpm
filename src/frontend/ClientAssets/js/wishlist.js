function initWishlist() {
  var pendingProducts = new Set();
  document.addEventListener("click", async function (event) {
    var button = event.target.closest("button.btn-wishlist[data-product-id]");
    if (!button) return;
    event.preventDefault();
    event.stopPropagation();

    var productId = button.dataset.productId;
    if (pendingProducts.has(productId)) return;
    pendingProducts.add(productId);
    var productName = button.closest(".product-card")?.querySelector(".card-body h3")?.textContent.trim();

    try {
      var response = await fetch("/Wishlist/Toggle/" + productId, {
        method: "POST",
        headers: { "RequestVerificationToken": antiForgeryToken() }
      });
      if (response.status === 401 || response.redirected) {
        window.location.href = "/Account/Login";
        return;
      }
      var data = await response.json();
      if (typeof data?.isWishlisted !== "boolean") {
        if (data?.message) showToast(data.message, "error");
        return;
      }
      document.querySelectorAll("button.btn-wishlist[data-product-id]").forEach(function (heart) {
        if (heart.dataset.productId !== productId) return;
        heart.classList.toggle("active", data.isWishlisted);
        heart.title = data.isWishlisted ? "Bỏ yêu thích" : "Yêu thích";
        heart.setAttribute("aria-pressed", String(data.isWishlisted));
      });
      var badge = document.getElementById("wishlist-count");
      if (badge) badge.textContent = data.count;
      var changed = new CustomEvent("wishlist:changed", {
        cancelable: true,
        detail: { saved: data.isWishlisted, productName: productName, message: data.message }
      });
      if (document.dispatchEvent(changed)) showToast(data.message, "success");
    } catch {
      showToast("Không thể cập nhật yêu thích. Vui lòng thử lại.", "error");
    } finally {
      pendingProducts.delete(productId);
    }
  });
}

function initHomeRecommendations() {
  var content = document.querySelector("[data-home-recommendations] [data-recommendation-url]");
  if (!content) return;
  var section = content.closest("[data-home-recommendations]");
  var status = section.querySelector("[data-recommendation-status]");
  var activeRequest;
  var accessoryToast;

  document.addEventListener("wishlist:changed", async function (event) {
    var detail = event.detail;
    if (detail.saved) event.preventDefault();
    accessoryToast?.remove();
    activeRequest?.abort();
    var request = new AbortController();
    activeRequest = request;
    content.setAttribute("aria-busy", "true");

    try {
      var response = await fetch(content.dataset.recommendationUrl, { signal: request.signal });
      if (!response.ok || response.redirected) throw new Error("Không thể tải gợi ý.");
      var markup = await response.text();
      if (request.signal.aborted) return;

      var focused = content.contains(document.activeElement) ? document.activeElement : null;
      content.innerHTML = markup;
      section.hidden = !content.firstElementChild;
      content.querySelectorAll(".reveal").forEach(function (card) { card.classList.add("revealed"); });
      initCartButtons(content);
      content.dispatchEvent(new Event("recommendations:updated", { bubbles: true }));

      if (focused) {
        var heart = focused.matches(".btn-wishlist")
          ? content.querySelector('button.btn-wishlist[data-product-id="' + CSS.escape(focused.dataset.productId) + '"]')
          : null;
        (heart || document.getElementById("recommendations-heading")).focus({ preventScroll: true });
      }
      var name = detail.productName;
      status.textContent = detail.saved && name
        ? "Đã cập nhật gợi ý sau khi bạn lưu " + name + "."
        : "Đã cập nhật gợi ý theo danh sách yêu thích của bạn.";
      if (detail.saved) {
        var accessoryHeading = content.querySelector("#home-accessory-heading");
        var action = accessoryHeading && accessoryHeading.dataset.suggestedFor === name
          ? { label: "Xem phụ kiện", href: "#" + accessoryHeading.id }
          : null;
        var notification = showToast(detail.message, "success", action);
        accessoryToast = action ? notification : null;
      }
    } catch {
      if (!request.signal.aborted) {
        status.textContent = "Tải lại trang để xem gợi ý mới.";
        if (detail.saved) showToast(detail.message, "success");
      }
    } finally {
      if (activeRequest === request) {
        content.removeAttribute("aria-busy");
        activeRequest = null;
      }
    }
  });
}
