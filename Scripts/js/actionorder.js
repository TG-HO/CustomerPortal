$(document).ready(function(){
    
    //cannot insert site name
    $("#code").prop("disabled",true);
    $("#test").blur(function(){
        if($("#test").val().length == 4){
            $("#code").prop("disabled",true);
        }
        else{
            $("#code").prop("disabled",false);
        }
    })
    if($("#test").val().length == 4){
        $("#code").prop("disabled",true);
    }
    else{
        $("#code").prop("disabled",false);
    }

    $('#btnorder').prop("disabled",true);
    $("#code").change(function(){
        var productvalue = $(this).val();
        // alert(productvalue);
              var  productname = $('#code').find(':selected').data('productname');
       

   $("#productname").val(productname);
   $("#btnorder").prop("disabled",false);
        
        // $.ajax({
        //     url: "/Order/GetProductName",
        //     method: "GET",
        //     data: {productvalue:productvalue},
        //     success: function(data){
        //         if(data == ""){
        //             $("#btnorder").prop("disabled",true);
        //             // $("#pas").prop("disabled",true);
        //             swal("Attention","Invalid product code","error");     
        //         }
                
        //         else{
        //             $("#productname").val(data);
        //             $("#btnorder").prop("disabled",false);
        //             // $("#pas").prop("disabled",false);

        //         }
        //     }
        // })

    })


});
