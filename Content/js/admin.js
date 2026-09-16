$(document).ready(function(){

    var prefix, detail, data1, bal, total,tl, siteInduction, whInduction, locInduction, tankcap, cgroup, customer, product;
   
    // prefix = $(".btnEdit").data('id');

    $(".btnEdit").click(function(){
        console.log($(this).val());
        prefix = $(this).data('id'); // order prefix

        // quantity
        detail = $(this).data('qid'); // holdfree

        // holdsfree
        data1 = $(this).parents('tr').find('td input.holds').val(); // release qty

        if( data1 > detail){
            // Swal.fire("Release Qty should be less than Holds free Qty");
            $(this).parents('tr').find('td div.warning-msg').slideDown().css('display', 'block');
            $(this).parents('tr').find('td input.holds').val("").focus();
        }else{
            
        //disabled approve button enable
            $(this).parents('tr').find('td > button.btnApprove').prop('disabled',false);
            
            $('.warning-msg').slideUp().hide();

            // $(this).parents('tr').find('td > button.btnApprove').click(function(){
            //     // $(this).parents('tr').find('td > input.holds').prop('disabled',true);
            //     data1 = $(this).parents('tr').find('td input.holds').val();
            //     if(data1 == ""){
            //         alert("kindly input holdsfree amount");
            //     }else{
            //         $(this).parents('tr').find('td > input.holds').prop('disabled',true);
                    
            //     }

            // });
            
            //checking null value of holdsfree input
            if(data1 == ""){
                Swal.fire("Null value of holdfree not allowed")
            }
            else{
                total = detail-data1;
                $(this).parents('tr').find('td.balance').text(total);
                // balance
                bal = $(this).parents('tr').find('td.balance').text();
            }  
        }}
    )    

    $(".btnApprove").parents('tr').find('td > button.btnApprove').click(function(){
        // $(this).parents('tr').find('td > input.holds').prop('disabled',true);

        // release qty
        data1 = $(this).parents('tr').find('td input.holds').val();

        // Tank lorry
        tl = $(this).parents('tr').find('td select.carrier').val();

        // site
        siteInduction = $(this).parents('tr').find('td select.site').val();

        // warehouse
        whInduction = $(this).parents('tr').find('td select.warehouse').val();

        // location
        locInduction = $(this).parents('tr').find('td select.location').val();

        // tank cap
        tankcap = $(this).parents('tr').find('td input.tcHidden').val();

        // cust group
        cgroup = $(this).parents('tr').find('td input.ciHidden').val();

        // customer
        customer = $(this).parents('tr').find('td input.cHidden').val();
          
        // product
        product = $(this).parents('tr').find('td input.productHidden').val();

        if(data1 == ""){
            Swal.fire("Kindly input holdsfree amount");
            bal = $(this).parents('tr').find('td.balance').text(""); // balance
        }else{
            $(this).prop('disabled', true); // approve button disable for specific row
            $(this).parents('tr').find('td > button.btnEdit').prop('disabled', true); // edit button disable for specific row
            $(this).parents('tr').find('td > input.holds').prop('disabled',true); // release 
            $(this).parents('tr').find('td select.carrier').prop('disabled',true); // tank lorry
            $(this).parents('tr').find('td select.site').prop('disabled', true); // site
            $(this).parents('tr').find('td select.warehouse').prop('disabled', true); // warehouse
            $(this).parents('tr').find('td select.location').prop('disabled', true); // location

            //console.log(tankcap + "" + cgroup + "" + data1 + "" + customer + "" + prefix);

            $.ajax({
                url: "/Admin/CheckDynRelease",
                method: "GET",
                data: {prefix:prefix,tankcap:tankcap,cgroup:cgroup,customer:customer,data1:data1,product:product},
                success: function(data){
                    var data = JSON.parse(data);
                    console.log(data);

                    if(data.status == "success")
                    {
                        // console.log('query exec');
                        $.ajax({
                            url: "/Admin/ApproveTransaction",
                            method: "GET",
                            data: {data1:data1,bal:bal,prefix:prefix,tl:tl,siteInduction:siteInduction,whInduction:whInduction,locInduction:locInduction},
                            success: function(data){
                                console.log(data);
                                var data = JSON.parse(data);
                                
                                if(data.status == "success")
                                {
                                    ajaxCallSpecial();
                                    // $.ajax({
                                    //     url: "actionNewRecord.php",
                                    //     method: "GET",
                                    //     data: {prefix:prefix, bal:bal, customer:customer, product:product},
                                    //     success: function(data){
                                    //         console.log(data);
                                    //         Swal.fire("Your data has been approved");     
                                    //     }
                                    // })
                                }
                            }
                        })
                    }
                    else
                    {
                        Swal.fire("Tank Capacity for " + data.customer + " per day limit has been exceeded.");   
                        $(this).prop('disabled', false); // approve button disable for specific row
                        $(this).parents('tr').find('td > button.btnEdit').prop('disabled', false); // edit button disable for specific row
                        $(this).parents('tr').find('td > input.holds').prop('disabled',false); // release 
                        $(this).parents('tr').find('td select.carrier').prop('disabled',false); // tank lorry
                        $(this).parents('tr').find('td select.site').prop('disabled', false); // site
                        $(this).parents('tr').find('td select.warehouse').prop('disabled', false); // warehouse
                        $(this).parents('tr').find('td select.location').prop('disabled', false); // location  
                        //console.log('nothing');                        
                    }
                }
            }) // end dynAjax

            function ajaxCallSpecial(){
                $.ajax({
                    url: "/Admin/CreateChildOrder",
                    method: "GET",
                    data: {prefix:prefix, bal:bal, customer:customer, product:product},
                    success: function(data){
                        console.log(data);
                    }
                })
            }
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


