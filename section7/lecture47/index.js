exports.handler = async (event) => {
    console.log("Event: \n" + JSON.stringify(event, null, 2));
    const response = {
        statusCode: 200,
        body: JSON.stringify(
            {
                message: "Hello from Lambda prepapired by Amir!"
            }
        )
    };
    return response;
}