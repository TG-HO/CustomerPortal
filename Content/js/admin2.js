$(document).ready(function(){

    var prefix, data1, bal, total, reqqty;
   
    // prefix = $(".btnEdit").data('id');

    $(".btnEdit").click(function(){
        console.log($(this).val());
        prefix = $(this).data('id'); // order prefix

        // req qty
        reqqty = $(this).data('rqid'); // required qty

        // holdsfree
        data1 = $(this).parents('tr').find('td input.holds').val(); // NEW HOLDSFREE qty

        if( data1 > reqqty){
            // Swal.fire("Release Qty should be less than Holds free Qty");
            $(this).parents('tr').find('td div.warning-msg').slideDown().css('display', 'block');
            $(this).parents('tr').find('td input.holds').val("").focus();
        }else{
            
        //disabled approve button enable
            $(this).parents('tr').find('td > button.btnApprove').prop('disabled',false);
            
            $('.warning-msg').slideUp().hide();
            
            //checking null value of holdsfree input
            if(data1 == ""){
                Swal.fire("Null value of holdfree not allowed")
            }
            else{
                total = reqqty-data1;
                $(this).parents('tr').find('td.balance').text(total);
                // balance
                bal = $(this).parents('tr').find('td.balance').text();
            }  
        }}
    )    

    $(".btnApprove").parents('tr').find('td > button.btnApprove').click(function(){
        // $(this).parents('tr').find('td > input.holds').prop('disabled',true);

        // new holdsfree qty
        data1 = $(this).parents('tr').find('td input.holds').val();

        if(data1 == ""){
            Swal.fire("Kindly input holdsfree amount");
            bal = $(this).parents('tr').find('td.balance').text(""); // balance
        }else{
            $(this).prop('disabled', true); // approve button disable for specific row
            $(this).parents('tr').find('td > button.btnEdit').prop('disabled', true); // edit button disable for specific row
            $(this).parents('tr').find('td > input.holds').prop('disabled',true); // new holdsfree qty 

            // console.log("prefix"+prefix+"\n"+"holdsfree"+data1+"\n"+"balance"+bal);
            Swal.fire({title:"Manual Holdsfree has been done",
                    //    confirmButtonText:"OK",
                    //    allowOutsideClick: false
                        });//.then((result=>{
                            // if(result.isConfirmed){
                            //     window.location.reload();
                            // }else{

                            // }
                        //}));     
            $.ajax({
                url: "/Admin/UpdateHoldsFreeQty",
                method: "GET",
                data: {data1:data1,bal:bal,prefix:prefix},
                success: function(data){
                    var data = JSON.parse(data);
                    // Swal.fire("Your data has been approved");     
                }

            })

        }

    });

    // $("#balance").click(function(){
    //     // Swal.fire({html:'Balance:  <?= number_format( $res2["Balance"],2);?> <br><br>  Credit Limit: <?= number_format( $res2["Credit_Limit"],2);?>',
    //     //             confirmButtonText:'Close'});
    //     // alert("hello world");
    // })

    //disable minus - key of keyboard
    $(".holds").keydown(function(e){
        if(!((e.keyCode > 95 && e.keyCode < 106)
        || (e.keyCode > 47 && e.keyCode < 58) 
        || e.keyCode == 8)) {
          return false;
      }
    })

    //balance credit debit
    $(".credit").click(function(){
        let credit= $(this).data('bid');
        $.ajax({
            url: "/Admin/GetCreditInfo",
            method: "GET",
            data: {credit:credit},
            success: function(data){
                var data = JSON.parse(data);
                console.log(data);
                Swal.fire({
                    html:`Balance:  ${data.balance} <br><br>  Credit Limit: ${data.credit}<br><br>  Available Credit Limit: ${data.available}`,
                    confirmButtonText:'Close'
                });
            }
        })

        
    })

});


