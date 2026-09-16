// login page script 

let info = document.getElementById("info");
let form = document.getElementById("form");
let logintbn = document.getElementById("loginbtn");
let bg = document.getElementById("bgImage");
let login = document.getElementById('loginNav');

function hide(){
    if (bg) {
        bg.style.boxShadow = "0 0 5000px black inset";
        bg.style.transition = "box-shadow 1s";
    }
    if (login) login.style.display = "none";
    if (form) {
        form.style.marginTop = "150px";
        form.style.boxShadow = "0 0 25px white";
        form.style.transition = "box-shadow 1s";
        form.style.visibility = "visible";
    }
    if (info) info.style.display = "none";    
    var loginBtnEl = document.getElementById("loginbtn");
    if (loginBtnEl) {
        loginBtnEl.style.display = "none";
    }
}

function customEffect(){
    if (form) {
        form.style.borderRadius = "15px";
        form.style.transform = "scale(1.1,1.1)";
        form.style.transition = "border-radius 1s, transform 1s";
    }
}

$(document).ready(function(){
    var lastQueriedId = "";
    var debounceTimer = null;

    function fetchUsername(showErrorOnFail) {
        var rawVal = $("#id").val();
        if (!rawVal) {
            $("#name").val("");
            return;
        }

        var idvalue = $.trim(rawVal);
        if (idvalue === "") {
            $("#name").val("");
            return;
        }

        var isCustomer = idvalue.toUpperCase().indexOf("TGPL") !== -1;
        var requestUrl = isCustomer ? "/Account/GetCustomerName" : "/Account/GetAdminName";

        $.ajax({
            url: requestUrl,
            method: "GET",
            data: { idvalue: idvalue },
            success: function(data){
                var trimmedData = $.trim(data);
                if (trimmedData === "") {
                    $("#name").val("");
                    $("#btnlogin").prop("disabled", true);
                    $("#pas").prop("disabled", true);
                    if (showErrorOnFail) {
                        var errorMsg = isCustomer ? "Invalid Customer ID" : "Invalid admin ID";
                        if (typeof swal === "function") {
                            swal({ title: "Attention", text: errorMsg, button: "X" });
                        } else {
                            alert(errorMsg);
                        }
                    }
                } else {
                    lastQueriedId = idvalue;
                    $("#name").val(trimmedData);
                    $("#btnlogin").prop("disabled", false);
                    $("#pas").prop("disabled", false);
                }
            },
            error: function(){
                // Fail-safe: ensure controls are not permanently locked on network blip
                $("#btnlogin").prop("disabled", false);
                $("#pas").prop("disabled", false);
            }
        });
    }

    // 1. Fetch on blur with error alert (original PHP behavior)
    $("#id").on("blur", function(){
        fetchUsername(true);
    });

    // 2. Fetch on change (e.g. browser autofill selection) without popup alert
    $("#id").on("change", function(){
        fetchUsername(false);
    });

    // 3. Fetch automatically on input while user types
    $("#id").on("input", function(){
        clearTimeout(debounceTimer);
        var val = $.trim($(this).val());
        if (val.length >= 3) {
            debounceTimer = setTimeout(function(){
                fetchUsername(false);
            }, 400);
        }
    });

    // 4. Automatically fetch immediately on page load if ID is already populated
    if ($("#id").val()) {
        fetchUsername(false);
    }

    // 5. Check shortly after page load for delayed browser autofill
    setTimeout(function(){
        if ($("#id").val() && !$("#name").val()) {
            fetchUsername(false);
        }
    }, 500);
});
