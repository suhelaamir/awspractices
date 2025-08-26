exports.handler = async (event, context) => {
    console.log("Remaining time: \n", context.getRemainingTimeInMillis());
    console.log("Function Name: \n", context.functionName);
    const body = `Function name: ${context.functionName}
    LogStream Name: ${context.logStreamName}
    `;
    return body;
}