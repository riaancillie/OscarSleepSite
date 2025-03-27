const dlgHtml = `
<div class="modal" tabindex="-1" id="dlgContact" data-bs-backdrop="static" data-bs-keyboard="false" tabindex="-1" aria-labelledby="staticBackdropLabel" aria-hidden="true">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header">
        <h5 class="modal-title">Bug Report</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
      </div>
      <div class="modal-body">
        <div class="pnlText">
            <div></div>
            <textarea class="form-control" style="height: 50vh"></textarea></div>
      </div>
      <div class="pnlTurnstile" style="text-align: center; margin-bottom: 50px;">
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
        <button type="button" class="btn btn-primary btnSend">Send</button>
      </div>
    </div>
  </div>
</div>
`

function showContactDialog(){
    var turnstileInstance = null;
    let dlg = $(dlgHtml);
    $("body").append(dlg);    
    $(dlg).modal("show");
    $(dlg).on("shown.bs.modal", () => { $("textarea", dlg).focus() });
    $(".btnSend", dlg).on("click", () => {
        let userText = $("textarea", dlg).val();
        if (userText.trim().length == 0) return;

        $(".pnlText", dlg).hide();
        $(".modal-footer", dlg).hide();
        turnstileInstance = turnstile.render($(".pnlTurnstile", dlg)[0], {
            sitekey: "0x4AAAAAABCSTEgOanOCCdfO",
            action: "data-upload",
            theme: "auto",
            size: window.innerWidth < 500 ? "compact" : "normal",
            callback: async function (token) {
                turnstileToken = token;
                turnstile.remove(turnstileInstance);

                let verifyResult = await fetch("/api/SendBugReport", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        turnstileToken: turnstileToken,
                        text: userText
                    })
                });

                $(dlg).modal("hide");
                $(dlg).remove();
            },
        });
    });
}