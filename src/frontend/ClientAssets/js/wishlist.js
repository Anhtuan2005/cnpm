function initWishlist() {
  document.querySelectorAll(".btn-wishlist[data-product-id]").forEach(function (btn) {
    btn.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      fetch("/Wishlist/Toggle/" + btn.dataset.productId, {
        method: "POST",
        headers: { "RequestVerificationToken": antiForgeryToken() }
      })
        .then(function (response) {
          if (response.status === 401 || response.redirected) {
            window.location.href = "/Account/Login";
            return null;
          }
          return response.json();
        })
        .then(function (data) {
          if (!data) return;
          btn.classList.toggle("active", data.isWishlisted);
          btn.title = data.isWishlisted ? "Bỏ yêu thích" : "Yêu thích";
          var badge = document.getElementById("wishlist-count");
          if (badge) badge.textContent = data.count;
          showToast(data.message, "success");
        });
    });
  });
}

