var lucideScript = document.getElementById("lucide-script");
if (lucideScript) lucideScript.addEventListener("load", refreshIcons);

document.addEventListener("DOMContentLoaded", function () {
  initFlashToasts();
  initCheckoutValidation();
  initNavbar();
  initCatalogFilters();
  initHeroSlider();
  initCountdown();
  initReveal();
  initCountUp();
  initProductInteractionTracking();
  initWishlist();
  initProductCompare();
  initCartOfferModal();
  initCartButtons();
  initCartPage();
  initCartSelection();
  initReviewForm();
  initReviewManagement();
  initReviewImagePreview();
  initVoucher();
  initQuantitySteppers();
  initAuthHelpers();
  initPaymentChoice();
  initSearchAutocomplete();
  initReviewFilters();
  initReviewPagination();
  initProductGallery();
  initProductTabs();
  initProductSpecModal();
  initDetailCoupon();
  initPdpNotes();
  initDetailAddToCart();
  initNotificationMenu();
  initOrderRouteMap();
  initOrderCancelDialog();
  initSupportRequestValidation();
  initAddressDropdowns();
  initProfileAddressFill();
  initAiChatbot();
  refreshIcons();
});

function refreshIcons() {
  if (window.lucide && typeof window.lucide.createIcons === "function") {
    window.lucide.createIcons({
      attrs: {
        "stroke-width": 1.5
      }
    });
  }
}

function antiForgeryToken() {
  var token = document.querySelector("[name=__RequestVerificationToken]");
  return token ? token.value : "";
}

function trackProductInteraction(productId, eventType) {
  if (!productId || !eventType) return;
  var body = new URLSearchParams();
  body.append("productId", productId);
  body.append("eventType", eventType);

  fetch("/Product/Track", {
    method: "POST",
    headers: {
      "Content-Type": "application/x-www-form-urlencoded",
      "RequestVerificationToken": antiForgeryToken()
    },
    body: body.toString(),
    keepalive: true
  }).catch(function () {});
}

function initProductInteractionTracking() {
  document.addEventListener("click", function (event) {
    var link = event.target.closest("[data-product-click]");
    if (!link) return;
    trackProductInteraction(link.dataset.productClick, "product_click");
  });
}

function postCartAdd(productId, quantity) {
  var body = new URLSearchParams();
  body.append("productId", productId);
  body.append("quantity", quantity || "1");
  return fetch("/Cart/Add", {
    method: "POST",
    headers: {
      "Content-Type": "application/x-www-form-urlencoded",
      "RequestVerificationToken": antiForgeryToken()
    },
    body: body.toString()
  }).then(function (response) {
    if (!response.ok) throw new Error("Cart request failed");
    return response.json();
  });
}

function updateCartBadges(data) {
  var count = data && data.itemCount;
  if (count === undefined || count === null) return;
  var cartCount = document.getElementById("cart-count");
  if (cartCount) cartCount.textContent = count;
  document.querySelectorAll(".cart-badge").forEach(function (badge) {
    badge.textContent = count;
  });
}

function getCartOfferSuggestions(data) {
  return data && Array.isArray(data.crossSellSuggestions) ? data.crossSellSuggestions : [];
}

function initCartOfferModal() {
  var modal = document.getElementById("cartOfferModal");
  var list = document.getElementById("cartOfferList");
  if (!modal || !list) return;
  var cartActions = modal.querySelector("[data-cart-offer-actions]");
  var buyNowActions = modal.querySelector("[data-buy-now-actions]");
  var continueButton = modal.querySelector("[data-buy-now-checkout]");
  var buyNowCheckoutUrl = null;
  var selectedAccessories = new Set();
  var returnFocus = null;

  function close() {
    modal.hidden = true;
    modal.setAttribute("aria-hidden", "true");
    document.body.classList.remove("cart-offer-open");
    if (returnFocus && returnFocus.isConnected) returnFocus.focus();
  }

  function escapeHtml(value) {
    return String(value === null || value === undefined ? "" : value)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  function render(suggestions) {
    list.innerHTML = suggestions.map(function (suggestion) {
      var hasDiscount = suggestion.hasDiscount === true;
      var badge = suggestion.badgeText || (hasDiscount ? "-" + suggestion.discountPercent + "%" : "Gợi ý");
      var price = hasDiscount
        ? '<span><del>' + escapeHtml(suggestion.originalPrice) + '</del><b>' + escapeHtml(suggestion.offerPrice) + '</b></span>'
        : '<span><b>' + escapeHtml(suggestion.originalPrice) + '</b></span>';
      return [
        '<article class="cart-offer-item">',
        '  <div class="cart-offer-media">',
        '    <img src="' + escapeHtml(suggestion.imageUrl || "/images/placeholder.svg") + '" alt="' + escapeHtml(suggestion.productName) + '" loading="lazy" decoding="async" />',
        '    <span>' + escapeHtml(badge) + '</span>',
        '  </div>',
        '  <div class="cart-offer-copy">',
        '    <strong>' + escapeHtml(suggestion.productName) + '</strong>',
        '    <small>' + escapeHtml(suggestion.contextText || ("Kèm " + suggestion.anchorProductName)) + '</small>',
        '    ' + price,
        '  </div>',
        '  <button class="cart-offer-add" type="button" data-cart-offer-add="' + escapeHtml(suggestion.productId) + '"'
          + (buyNowCheckoutUrl ? ' aria-pressed="false"' : '') + '>',
        '    <i data-lucide="plus" aria-hidden="true"></i>',
        buyNowCheckoutUrl ? '    Chọn mua kèm' : '    Thêm',
        '  </button>',
        '</article>'
      ].join("");
    }).join("");
    refreshIcons();
  }

  function open(suggestions, checkoutUrl, trigger) {
    if (!Array.isArray(suggestions) || suggestions.length === 0) return false;
    buyNowCheckoutUrl = checkoutUrl || null;
    selectedAccessories.clear();
    cartActions.hidden = !!buyNowCheckoutUrl;
    buyNowActions.hidden = !buyNowCheckoutUrl;
    continueButton.textContent = "Tiếp tục thanh toán";
    modal.querySelector("[data-cart-offer-eyebrow]").textContent = buyNowCheckoutUrl ? "Trước khi thanh toán" : "Đã thêm vào giỏ";
    modal.querySelector("#cartOfferTitle").textContent = buyNowCheckoutUrl ? "Phụ kiện mua kèm ưu đãi" : "Gợi ý mua kèm phù hợp";
    modal.querySelector("[data-cart-offer-description]").textContent = buyNowCheckoutUrl
      ? "Chọn phụ kiện phù hợp hoặc tiếp tục chỉ mua sản phẩm đã chọn. Mỗi phụ kiện có số lượng 1."
      : "Chọn phụ kiện phù hợp với giá ưu đãi khi mua cùng điện thoại.";
    returnFocus = trigger || document.activeElement;
    render(suggestions);
    modal.hidden = false;
    modal.setAttribute("aria-hidden", "false");
    document.body.classList.add("cart-offer-open");
    var closeButton = modal.querySelector("[data-cart-offer-close]");
    if (closeButton) closeButton.focus();
    return true;
  }

  window.showCartOfferModal = function (suggestions) {
    return open(suggestions, null);
  };

  window.showBuyNowOfferModal = function (suggestions, checkoutUrl, trigger) {
    return open(suggestions, checkoutUrl, trigger);
  };

  modal.querySelector("[data-buy-now-skip]").addEventListener("click", function () {
    if (buyNowCheckoutUrl) window.location.href = buyNowCheckoutUrl;
  });
  continueButton.addEventListener("click", function () {
    if (!buyNowCheckoutUrl) return;
    var url = new URL(buyNowCheckoutUrl, window.location.origin);
    selectedAccessories.forEach(function (id) { url.searchParams.append("buyNowAccessoryIds", id); });
    window.location.href = url.pathname + url.search;
  });

  modal.querySelectorAll("[data-cart-offer-close]").forEach(function (button) {
    button.addEventListener("click", close);
  });

  list.addEventListener("click", function (event) {
    var button = event.target.closest("[data-cart-offer-add]");
    if (!button || button.disabled) return;
    if (buyNowCheckoutUrl) {
      var id = button.dataset.cartOfferAdd;
      var selected = !selectedAccessories.has(id);
      if (selected) selectedAccessories.add(id);
      else selectedAccessories.delete(id);
      button.setAttribute("aria-pressed", String(selected));
      button.innerHTML = '<i data-lucide="' + (selected ? 'check' : 'plus') + '" aria-hidden="true"></i>'
        + (selected ? "Đã chọn" : "Chọn mua kèm");
      continueButton.textContent = "Tiếp tục thanh toán" + (selectedAccessories.size ? " (" + selectedAccessories.size + " phụ kiện)" : "");
      refreshIcons();
      return;
    }
    button.disabled = true;
    button.dataset.originalHtml = button.dataset.originalHtml || button.innerHTML;
    button.textContent = "Đang thêm";
    postCartAdd(button.dataset.cartOfferAdd, "1")
      .then(function (data) {
        if (!data.success) {
          showToast(data.message || "Không thể thêm sản phẩm vào giỏ.", "error");
          return;
        }
        updateCartBadges(data);
        showToast(data.message, "success");
        var suggestions = getCartOfferSuggestions(data);
        if (suggestions.length) {
          render(suggestions);
        } else {
          close();
        }
      })
      .catch(function () {
        showToast("Không thể thêm sản phẩm vào giỏ. Vui lòng thử lại.", "error");
      })
      .finally(function () {
        button.disabled = false;
        if (button.dataset.originalHtml) {
          button.innerHTML = button.dataset.originalHtml;
          refreshIcons();
        }
      });
  });

  document.addEventListener("keydown", function (event) {
    if (modal.hidden) return;
    if (event.key === "Escape") close();
    if (event.key !== "Tab") return;
    var focusable = Array.from(modal.querySelectorAll('button:not(:disabled), a[href]'))
      .filter(function (element) { return element.getClientRects().length > 0; });
    var first = focusable[0];
    var last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  });
}

function showCartOffersAfterAdd(data) {
  var suggestions = getCartOfferSuggestions(data);
  if (window.showCartOfferModal && window.showCartOfferModal(suggestions)) {
    return true;
  }
  return false;
}

