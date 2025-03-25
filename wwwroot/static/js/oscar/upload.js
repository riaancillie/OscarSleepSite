let uploadFile = null
let emailAddr = null;
let turnstileToken = null;
let turnstileInstance = null;

function showDragDropPanel(){
    $("#pnlUpload .dropbox").click((evt) => {
        let rect = $("#pnlUpload .upload-box")[0].getBoundingClientRect();

        if (evt.clientX >= rect.x && evt.clientX <= rect.right && evt.clientY >= rect.y && evt.clientY <= rect.bottom)
        {
            $("#txtFileSelect").click();
        }
    });

    $("#txtFileSelect").on("change", async (evt) => {
        if (validateFile($("#txtFileSelect")[0])){
            uploadFile = $("#txtFileSelect")[0].files[0];
            showCaptureEmail();
        } else {
            let anim = $("#pnlUpload .anim-error")[0];
            anim.seek(0);
            $("#pnlUpload .anim-error").removeClass("hidden");
            anim.play();
            $("#pnlUpload .anim-error").removeClass("hidden");
        }
    });


    let showDragOk = () => {
        $("#pnlUpload .anim-error").addClass("hidden");

        let anim = $("#pnlUpload .anim-dragok")[0];
        anim.seek(0);
        anim.play();

        $("#pnlUpload .anim-dragok").removeClass("hidden");
    };

    let busyDragging = () => {
        return !$("#pnlUpload .anim-error").hasClass("hidden") ||
            !$("#pnlUpload .anim-dragok").hasClass("hidden")
    }

    let validateFile = (event) => {
        if (event.dataTransfer && event.dataTransfer.items && event.dataTransfer.items.length == 1){
            let itm = event.dataTransfer.items[0];
            if (itm.type == "application/x-gzip"){
                let file = itm.getAsFile();
                if (file == null) return true;
                console.log(file);
                if (file.name.endsWith(".json.gz")) return true;
            }

        } else if (event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files == 1) {
            let file = event.dataTransfer.files[0];
            if (file.name.endsWith(".json.gz")){
                return true;
            }
        } else if (event.files.length == 1){
            let file = event.files[0];
            if (file.name.endsWith(".json.gz")){
                return true;
            }
        }
        return false;
    }

    $("#pnlUpload .dropbox").on("dragover", (evt) => {

        let event = evt.originalEvent;
        event.preventDefault();

        //$("#pnlUpload .upload-container, #pnlUpload .upload-box,#pnlUpload .upload-box > *").css("pointer-events", "none");

        if (busyDragging())
            return;


        if (validateFile(event)){
            showDragOk();
        }  else {
            let anim = $("#pnlUpload .anim-error")[0];
            anim.seek(0);
            $("#pnlUpload .anim-error").removeClass("hidden");
            anim.play();
        }

    });

    $("#pnlUpload .dropbox").on("drop", async (evt) => {
        let event = evt.originalEvent;
        event.preventDefault();
        if (!validateFile(event)) return;


        let file = null;
        if (event.dataTransfer.items && event.dataTransfer.items.length == 1){
            file = event.dataTransfer.items[0].getAsFile();
        } else if (event.dataTransfer.files && event.dataTransfer.files == 1) {
             file = event.dataTransfer.files[0];
        }
        uploadFile = file;


        showCaptureEmail();
    });

    $("#pnlUpload .dropbox").on("dragleave", (evt) => {
        let e = evt.originalEvent;
        let event = evt.originalEvent;
        event.preventDefault();
        $("#pnlUpload .anim-error").addClass("hidden");
        $("#pnlUpload .anim-dragok").addClass("hidden");

    });

    $("#pnlUpload").removeClass("hidden");
}

function fadeOut(target, duration){
    return new Promise(res => {
        $(target).fadeOut(duration, () => {res();});
    });
}


async function showCaptureEmail(){
    await fadeOut($("#pnlUpload"));
    $("#pnlCaptureEmail").removeClass("hidden").css("display", "none").fadeIn(null, () => {$("#pnlCaptureEmail input").focus()});

}

async function onSubmitCaptureEmail(){
    if ($("#pnlCaptureEmail input")[0].checkValidity()){
        emailAddr = $("#pnlCaptureEmail input").val();
        showTurnstile();
    }
}

function delay(ms){ return new Promise(res => {setTimeout(res, ms); })}

async function showTurnstile(){
    await fadeOut($("#pnlCaptureEmail"));
    $("#pnlTurnstile").removeClass("hidden").css("display", "none").fadeIn(() => {
        turnstileInstance = turnstile.render("#pnlTurnstile .dotted-box", {
            sitekey: "0x4AAAAAABCSTEgOanOCCdfO",
            action: "data-upload",
            theme: "auto",
            size: window.innerWidth < 500 ? "compact" : "normal",
            callback: async function (token) {
                turnstileToken = token;
                turnstile.remove(turnstileInstance);
                await delay(500);
                showUpload();
            },
        });
    });
}

async function showUpload(){
    await fadeOut($("#pnlTurnstile"));
    $("#pnlProgress").removeClass("hidden").css("display", "none").fadeIn();
    let anim = $("#pnlProgress .anim-upload")[0];
    anim.seek(0);

    let onUploadProgress = (progressEvent) => {
        const totalLength = progressEvent.lengthComputable ? progressEvent.total : progressEvent.target.getResponseHeader('content-length') || progressEvent.target.getResponseHeader('x-decompressed-content-length');        
        if (totalLength !== null) {            
            anim.seek(Math.round((progressEvent.loaded / totalLength) * 30));            
        }
    };

    try{
        let formData = new FormData();
        formData.append("file", uploadFile, "data.json.gz");

        let postResult = await axios.post("/api/UploadSharedSession", formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
                "X-TURNSTILE-TOKEN": turnstileToken,
                "X-SENDER-EMAIL": emailAddr
            },
            onUploadProgress: onUploadProgress
        });        

        if (postResult.status != 200){
            throw new Error(postResult);
        }

        anim.play();
        await delay(1500);
        showOkPanel();
        
    }catch (err){
        console.error(err);
        showErrorPanel();
        throw err;
    }


}


async function showOkPanel(){
    await fadeOut($("#pnlProgress"));
    $("#pnlResultOK").removeClass("hidden").css("display", "none").fadeIn();
    let anim = $("#pnlResultOK .anim-email")[0];
    anim.seek(0);
    anim.play();
}


async function showErrorPanel(){
    await fadeOut($("#pnlProgress"));
    $("#pnlResultFail").removeClass("hidden").css("display", "none").fadeIn();
    let anim = $("#pnlResultFail .anim-error")[0];
    anim.seek(0);
    anim.play();
}

async function main(){
    var locale = window.navigator.userLanguage || window.navigator.language;
    moment.locale(locale);
    showDragDropPanel();
}

