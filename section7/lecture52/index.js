exports.handler = async (event) => {
    console.log("Event: \n" + JSON.stringify(event, null, 2));
    const total = event.num1 + event.num2;
    console.log("Total: " + total);
    const response = {
        statusCode: 200,
        body: JSON.stringify(
            {
                message: "The total of " + event.num1 + " and " + event.num2 + " is " + total
            }
        )
    };
    return response;
}