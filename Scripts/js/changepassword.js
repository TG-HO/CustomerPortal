$(document).ready(function(){
    let oldpass,newpass,confirmpass;
    $("#oldpass").blur(function(){
        oldpass = $(this).val();
        $.ajax({
            url: "/Account/VerifyCurrentPassword",
            method: "GET",
            data: {oldpass:oldpass},
            success: function(data){
                var data = JSON.parse(data);
                // console.log(data);
                if(data.message == "success"){
                    $("#newpass").prop('disabled',false);
                    // $("#confirmpass").prop('disabled',false);
                    // $("#submit").prop('disabled',true);

                }else{
                    Swal.fire("Enter correct password");
                    $("#newpass").prop('disabled',true)
                    // $("#confirmpass").prop('disabled',true);
                }
            }

        })

    })

    //checking new pass with confirm pass
    // newpass = $("#newpass").val();
    // confirmpass = $("#confirmpass").val();
    // if(newpass == confirmpass){
    //     $("#submit").prop('disabled',false);
    // }else{
    //     $("#submit").prop('disabled',true);
    // }


    $("#newpass").blur(function(){
        newpass = $(this).val();
        // alert(newpass);
        $("#confirmpass").prop('disabled',false);
        $("#confirmpass").blur(function(){
            confirmpass = $(this).val();
            // alert(confirmpass);
            if(newpass == confirmpass){
                $("#submit").prop('disabled',false);
            }else{
                Swal.fire("Password do not match");
                $("#submit").prop('disabled',true);
            }
        })


    })


})