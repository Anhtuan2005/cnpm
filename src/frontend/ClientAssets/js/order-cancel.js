function initOrderCancelDialog() {
  var dialog = document.querySelector("[data-order-cancel-dialog]");
  if (!dialog || typeof dialog.showModal !== "function") return;

  var forms = document.querySelectorAll("[data-order-cancel-form]");
  var code = dialog.querySelector("[data-order-cancel-code]");
  var confirmButton = dialog.querySelector("[data-order-cancel-confirm]");
  var pendingForm = null;

  forms.forEach(function (form) {
    // Retain the browser confirmation as a fallback when JavaScript is unavailable.
    form.removeAttribute("onsubmit");
    form.addEventListener("submit", function (event) {
      if (form.dataset.orderCancelConfirmed === "true") return;
      event.preventDefault();
      pendingForm = form;
      code.textContent = form.dataset.orderCode || "";
      dialog.showModal();
      document.body.classList.add("order-cancel-open");
    });
  });

  dialog.querySelectorAll("[data-order-cancel-close]").forEach(function (button) {
    button.addEventListener("click", function () { dialog.close(); });
  });

  dialog.addEventListener("click", function (event) {
    if (event.target === dialog) dialog.close();
  });

  dialog.addEventListener("close", function () {
    pendingForm = null;
    document.body.classList.remove("order-cancel-open");
  });

  confirmButton.addEventListener("click", function () {
    if (!pendingForm) return;
    var form = pendingForm;
    form.dataset.orderCancelConfirmed = "true";
    confirmButton.disabled = true;
    dialog.close();
    form.requestSubmit();
  });
}
