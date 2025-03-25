
function delay(ms){ return new Promise(res => {setTimeout(res, ms); })}

function fadeOut(target, duration){
    return new Promise(res => {
        $(target).fadeOut(duration, () => {res();});
    });
}


async function showOkPanel(shortCode){
    await fadeOut($("#pnlProcessing"));
    $("#pnlResultOK").removeClass("hidden").css("display", "none").fadeIn();
    let shortcodeUrl = "https://oscarsleep.org/view/"+shortCode;
    $("#txtShortCode").val(shortcodeUrl);
    $("#btnCopy").on("click", () => {
        navigator.clipboard.writeText(shortcodeUrl);
    });
}


async function showErrorPanel(errorText){
    await fadeOut($("#pnlProcessing"));
    $("#pnlResultFail").removeClass("hidden").css("display", "none").fadeIn();
    let anim = $("#pnlResultFail .anim-error")[0];
    anim.seek(0);
    anim.play();
}

async function main(){
    try{
        let verifyTokenEncoded = location.pathname.split("/").reverse()[0];
        let verifyToken = decodeURIComponent(verifyTokenEncoded);
        let verifyResult = await fetch("/api/VerifySharedSession", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({token: verifyToken})
        });

        let resultJson = await verifyResult.json();
        if (!resultJson.ok){
            showErrorPanel(resultJson.error)
        } else {
            showOkPanel(resultJson.shortCode);
        }
    } catch {
        showErrorPanel("");
    }
}

