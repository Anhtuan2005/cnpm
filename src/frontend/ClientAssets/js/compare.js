function initProductCompare() {
  var storageKey = "techvora-compare-products-v2";
  var tray = document.querySelector("[data-compare-tray]");
  var count = tray ? tray.querySelector("[data-compare-count]") : null;
  var hint = tray ? tray.querySelector("[data-compare-hint]") : null;
  var itemsRoot = tray ? tray.querySelector("[data-compare-items]") : null;
  var clearButton = tray ? tray.querySelector("[data-compare-clear]") : null;
  var openLink = tray ? tray.querySelector("[data-compare-open]") : null;
  var onComparePage = !!document.querySelector(".compare-page");

  function normalize(items) {
    if (!Array.isArray(items)) return [];
    var seen = {};
    return items.filter(function (item) {
      var id = Number(item && item.id);
      if (!id || seen[id]) return false;
      seen[id] = true;
      item.id = id;
      item.name = String(item.name || "Sản phẩm");
      item.image = String(item.image || "/images/placeholder.svg");
      item.categoryId = Number(item.categoryId) || 0;
      item.categoryName = String(item.categoryName || "danh mục này");
      item.categorySlug = String(item.categorySlug || "");
      if (!item.categorySlug && /^điện thoại(?:\s|$)/i.test(item.categoryName)) item.categorySlug = "dien-thoai";
      return true;
    }).slice(0, 2);
  }

  function readSelection() {
    try {
      return normalize(JSON.parse(window.localStorage.getItem(storageKey) || "[]"));
    } catch (error) {
      return [];
    }
  }

  var selected = readSelection();
  var seeded = normalize([].slice.call(document.querySelectorAll("[data-compare-seed]")).map(function (element) {
    return {
      id: element.dataset.compareSeed,
      name: element.dataset.compareName,
      image: element.dataset.compareImage,
      categoryId: element.dataset.compareCategoryId,
      categoryName: element.dataset.compareCategoryName,
      categorySlug: element.dataset.compareCategorySlug
    };
  }));
  if (seeded.length > 0) selected = seeded;

  function writeSelection() {
    try {
      window.localStorage.setItem(storageKey, JSON.stringify(selected));
    } catch (error) {
      // Query-string navigation still works when local storage is unavailable.
    }
  }

  function buildCompareUrl() {
    var params = new URLSearchParams();
    selected.forEach(function (item) { params.append("ids", String(item.id)); });
    return "/Product/Compare?" + params.toString();
  }

  function isPhoneCategory(slug) {
    slug = String(slug || "").toLowerCase();
    return slug === "dien-thoai" || slug.indexOf("dien-thoai-") === 0;
  }

  function canCompare(first, categoryId, categorySlug) {
    return first.categoryId === categoryId ||
      (isPhoneCategory(first.categorySlug) && isPhoneCategory(categorySlug));
  }

  function renderItems() {
    if (!itemsRoot) return;
    itemsRoot.replaceChildren();
    for (var index = 0; index < 2; index++) {
      var item = selected[index];
      if (!item) {
        var empty = document.createElement("span");
        empty.className = "compare-tray-slot is-empty";
        empty.setAttribute("aria-hidden", "true");
        itemsRoot.appendChild(empty);
        continue;
      }

      var slot = document.createElement("span");
      slot.className = "compare-tray-slot";
      slot.title = item.name;

      var image = document.createElement("img");
      image.src = item.image;
      image.alt = "";
      image.loading = "lazy";
      image.decoding = "async";
      slot.appendChild(image);

      var remove = document.createElement("button");
      remove.type = "button";
      remove.dataset.compareRemove = String(item.id);
      remove.setAttribute("aria-label", "Bỏ " + item.name + " khỏi so sánh");
      remove.innerHTML = '<i data-lucide="x" aria-hidden="true"></i>';
      slot.appendChild(remove);
      itemsRoot.appendChild(slot);
    }
  }

  function render() {
    document.querySelectorAll("[data-compare-product]").forEach(function (button) {
      var active = selected.some(function (item) { return item.id === Number(button.dataset.compareProduct); });
      button.classList.toggle("active", active);
      button.setAttribute("aria-pressed", String(active));
      button.title = active ? "Bỏ khỏi so sánh" : "Thêm vào so sánh";
    });

    if (!tray) return;
    tray.hidden = onComparePage || selected.length === 0;
    document.body.classList.toggle("compare-tray-visible", !onComparePage && selected.length > 0);
    if (count) count.textContent = String(selected.length);
    if (hint) {
      hint.textContent = selected.length < 2
        ? "Chọn thêm một điện thoại bất kỳ"
        : "Sẵn sàng so sánh hai điện thoại";
    }
    if (openLink) {
      var disabled = selected.length < 2;
      openLink.href = buildCompareUrl();
      openLink.classList.toggle("disabled", disabled);
      openLink.setAttribute("aria-disabled", String(disabled));
      openLink.tabIndex = disabled ? -1 : 0;
    }
    renderItems();
    refreshIcons();
  }

  document.querySelectorAll("[data-compare-product]").forEach(function (button) {
    button.addEventListener("click", function (event) {
      event.preventDefault();
      event.stopPropagation();
      var id = Number(button.dataset.compareProduct);
      var existingIndex = selected.findIndex(function (item) { return item.id === id; });
      if (existingIndex >= 0) {
        selected.splice(existingIndex, 1);
      } else if (selected.length >= 2) {
        showToast("Bạn chỉ có thể so sánh 2 sản phẩm.", "error");
        return;
      } else {
        var categoryId = Number(button.dataset.compareCategoryId);
        var categorySlug = String(button.dataset.compareCategorySlug || "");
        if (selected.length > 0 && !canCompare(selected[0], categoryId, categorySlug)) {
          showToast("Chỉ có thể so sánh các sản phẩm cùng loại.", "error");
          return;
        }
        selected.push({
          id: id,
          name: button.dataset.compareName || "Sản phẩm",
          image: button.dataset.compareImage || "/images/placeholder.svg",
          categoryId: categoryId,
          categoryName: button.dataset.compareCategoryName || "danh mục này",
          categorySlug: categorySlug
        });
      }
      writeSelection();
      render();
    });
  });

  if (itemsRoot) {
    itemsRoot.addEventListener("click", function (event) {
      var remove = event.target.closest("[data-compare-remove]");
      if (!remove) return;
      selected = selected.filter(function (item) { return item.id !== Number(remove.dataset.compareRemove); });
      writeSelection();
      render();
    });
  }

  document.querySelectorAll("[data-compare-remove-id]").forEach(function (link) {
    link.addEventListener("click", function () {
      selected = selected.filter(function (item) { return item.id !== Number(link.dataset.compareRemoveId); });
      writeSelection();
    });
  });

  if (clearButton) {
    clearButton.addEventListener("click", function () {
      selected = [];
      writeSelection();
      render();
    });
  }

  document.querySelectorAll("[data-compare-clear-link]").forEach(function (link) {
    link.addEventListener("click", function () {
      selected = [];
      writeSelection();
    });
  });

  if (openLink) {
    openLink.addEventListener("click", function (event) {
      if (selected.length >= 2) return;
      event.preventDefault();
      showToast("Chọn ít nhất 2 sản phẩm để so sánh.", "error");
    });
  }

  writeSelection();
  render();
}
